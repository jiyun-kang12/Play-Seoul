import argparse
import concurrent.futures
import json
import os
import re
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional

try:
    import requests
except ImportError as exc:
    raise SystemExit(
        "The 'requests' package is required. Install it with: python -m pip install requests"
    ) from exc


SUBMIT_URL = "https://openapi.ai.nc.com/3d/varco/v1/image-to-3d"
RESULT_URL = "https://openapi.ai.nc.com/inference/result/{request_id}"


@dataclass(frozen=True)
class VarcoJob:
    image: Path
    output: Path
    target_face_type: str
    target_face_num: int
    generate_texture: bool
    seed: int


def find_project_root(start: Path) -> Path:
    for candidate in [start, *start.parents]:
        if (candidate / ".env").exists() and (candidate / "Assets").exists():
            return candidate
    return Path.cwd()


def load_env_file(path: Path) -> Dict[str, str]:
    values: Dict[str, str] = {}
    if not path.exists():
        return values

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        values[key] = value
    return values


def get_api_key(project_root: Path, explicit_key: Optional[str]) -> str:
    if explicit_key:
        return explicit_key

    env_values = load_env_file(project_root / ".env")
    key = os.environ.get("OPENAPI_KEY") or env_values.get("OPENAPI_KEY")
    if not key:
        raise RuntimeError(
            "OPENAPI_KEY was not found. Set it in .env or pass --api-key."
        )
    return key


def safe_stem(path: Path) -> str:
    stem = re.sub(r"[^A-Za-z0-9._-]+", "_", path.stem).strip("._-")
    return stem or "varco_model"


def bool_string(value: bool) -> str:
    return "true" if value else "false"


def submit_job(job: VarcoJob, api_key: str, timeout: int) -> str:
    headers = {"OPENAPI_KEY": api_key}
    data = {
        "target_face_type": job.target_face_type,
        "target_face_num": str(job.target_face_num),
        "generate_texture": bool_string(job.generate_texture),
        "seed": str(job.seed),
    }

    with job.image.open("rb") as image_file:
        response = requests.post(
            SUBMIT_URL,
            headers=headers,
            files={"image": image_file},
            data=data,
            timeout=timeout,
        )

    response.raise_for_status()
    payload = response.json()
    request_id = payload.get("requestId")
    if not request_id:
        raise RuntimeError(f"Missing requestId in VARCO response: {payload}")
    return str(request_id)


def poll_result(
    request_id: str,
    api_key: str,
    poll_interval: float,
    timeout_seconds: int,
    request_timeout: int,
) -> Dict[str, Any]:
    headers = {"OPENAPI_KEY": api_key}
    started_at = time.time()

    while True:
        response = requests.get(
            RESULT_URL.format(request_id=request_id),
            headers=headers,
            timeout=request_timeout,
        )
        response.raise_for_status()
        payload = response.json()
        status = str(payload.get("status", "")).lower()

        if status != "processing":
            return payload

        if time.time() - started_at > timeout_seconds:
            raise TimeoutError(
                f"Timed out waiting for VARCO request {request_id} after {timeout_seconds}s"
            )
        time.sleep(poll_interval)


def download_model(model_url: str, output: Path, request_timeout: int) -> int:
    output.parent.mkdir(parents=True, exist_ok=True)
    with requests.get(model_url, stream=True, timeout=request_timeout) as response:
        response.raise_for_status()
        with output.open("wb") as file:
            total = 0
            for chunk in response.iter_content(chunk_size=1024 * 1024):
                if not chunk:
                    continue
                file.write(chunk)
                total += len(chunk)
    return total


def run_job(
    job: VarcoJob,
    api_key: str,
    poll_interval: float,
    timeout_seconds: int,
    request_timeout: int,
) -> Dict[str, Any]:
    if not job.image.exists():
        raise FileNotFoundError(f"Image not found: {job.image}")

    print(f"[submit] {job.image} -> {job.output}", flush=True)
    request_id = submit_job(job, api_key, request_timeout)
    print(f"[poll] {job.image.name}: requestId={request_id}", flush=True)
    result = poll_result(request_id, api_key, poll_interval, timeout_seconds, request_timeout)

    status = str(result.get("status", "")).lower()
    if status not in {"success", "completed", "complete", "done", "succeeded"}:
        model_url = result.get("model_url")
        if not model_url:
            raise RuntimeError(f"VARCO job failed or returned no model_url: {result}")
    else:
        model_url = result.get("model_url")

    if not model_url:
        raise RuntimeError(f"Missing model_url in VARCO result: {result}")

    size = download_model(str(model_url), job.output, request_timeout)
    print(f"[done] {job.output} ({size / (1024 * 1024):.2f} MB)", flush=True)

    return {
        "image": str(job.image),
        "output": str(job.output),
        "requestId": request_id,
        "status": result.get("status"),
        "model_url": model_url,
        "bytes": size,
        "target_face_type": job.target_face_type,
        "target_face_num": job.target_face_num,
        "generate_texture": job.generate_texture,
        "seed": job.seed,
    }


def load_plan(plan_path: Path) -> List[Dict[str, Any]]:
    payload = json.loads(plan_path.read_text(encoding="utf-8"))
    if isinstance(payload, list):
        return payload
    if isinstance(payload, dict) and isinstance(payload.get("jobs"), list):
        return payload["jobs"]
    raise ValueError("Plan must be a JSON array or an object with a 'jobs' array.")


def make_jobs(args: argparse.Namespace, project_root: Path) -> List[VarcoJob]:
    output_dir = Path(args.output_dir)
    if not output_dir.is_absolute():
        output_dir = project_root / output_dir

    raw_jobs: List[Dict[str, Any]] = []
    if args.plan:
        raw_jobs.extend(load_plan(Path(args.plan)))
    for image in args.image:
        raw_jobs.append({"image": image})

    if not raw_jobs:
        raise ValueError("Provide at least one --image or a --plan JSON file.")

    jobs: List[VarcoJob] = []
    for raw in raw_jobs:
        image = Path(str(raw["image"]))
        if not image.is_absolute():
            image = project_root / image

        output_value = raw.get("output")
        if output_value:
            output = Path(str(output_value))
            if not output.is_absolute():
                output = project_root / output
        else:
            output = output_dir / f"{safe_stem(image)}.glb"

        jobs.append(
            VarcoJob(
                image=image,
                output=output,
                target_face_type=str(raw.get("target_face_type", args.target_face_type)),
                target_face_num=int(raw.get("target_face_num", args.target_face_num)),
                generate_texture=str(raw.get("generate_texture", args.generate_texture)).lower()
                in {"1", "true", "yes", "y"},
                seed=int(raw.get("seed", args.seed)),
            )
        )
    return jobs


def write_manifest(output_dir: Path, records: Iterable[Dict[str, Any]]) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    manifest_path = output_dir / "varco_manifest.json"
    manifest = {
        "generated_at": time.strftime("%Y-%m-%dT%H:%M:%S%z"),
        "records": list(records),
    }
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"[manifest] {manifest_path}", flush=True)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate GLB models through VARCO Image-to-3D and save them into Unity Assets."
    )
    parser.add_argument(
        "--image",
        action="append",
        default=[],
        help="PNG image path. Can be passed multiple times.",
    )
    parser.add_argument(
        "--plan",
        help="Optional JSON plan with jobs: [{image, output?, target_face_type?, target_face_num?, generate_texture?, seed?}]",
    )
    parser.add_argument(
        "--output-dir",
        default="Assets/00 Ghost Station",
        help="Directory for generated .glb files. Default: Assets/00 Ghost Station",
    )
    parser.add_argument("--target-face-type", choices=["tri", "quad"], default="tri")
    parser.add_argument("--target-face-num", type=int, default=300000)
    parser.add_argument("--generate-texture", default="true", choices=["true", "false"])
    parser.add_argument("--seed", type=int, default=-1)
    parser.add_argument("--concurrency", type=int, default=2)
    parser.add_argument("--poll-interval", type=float, default=2.0)
    parser.add_argument("--timeout-seconds", type=int, default=20 * 60)
    parser.add_argument("--request-timeout", type=int, default=120)
    parser.add_argument("--api-key", help="Optional API key override. Prefer .env OPENAPI_KEY.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    project_root = find_project_root(Path(__file__).resolve())
    api_key = get_api_key(project_root, args.api_key)
    jobs = make_jobs(args, project_root)

    output_dir = Path(args.output_dir)
    if not output_dir.is_absolute():
        output_dir = project_root / output_dir

    output_dir.mkdir(parents=True, exist_ok=True)
    records: List[Dict[str, Any]] = []
    failures: List[str] = []

    max_workers = max(1, min(args.concurrency, len(jobs)))
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        future_to_job = {
            executor.submit(
                run_job,
                job,
                api_key,
                args.poll_interval,
                args.timeout_seconds,
                args.request_timeout,
            ): job
            for job in jobs
        }

        for future in concurrent.futures.as_completed(future_to_job):
            job = future_to_job[future]
            try:
                records.append(future.result())
            except Exception as exc:
                message = f"{job.image} -> {job.output}: {exc}"
                failures.append(message)
                print(f"[error] {message}", file=sys.stderr, flush=True)

    write_manifest(output_dir, records)
    if failures:
        print("\nFailures:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
