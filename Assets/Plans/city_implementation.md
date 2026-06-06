# Project Overview
- Game Title: Seoul Ghost Line (Implicit from context)
- High-Level Concept: Implementation of a city environment around Old Seoul Station based on provided perspective images (X, Z, -X directions).
- Players: Single player (Player character exists in scene).
- Inspiration / Reference Games: Real-world Seoul Station area and Seoullo 7017.
- Tone / Art Direction: Realistic Urban / Modern Seoul.
- Target Platform: PC (StandaloneWindows64).
- Render Pipeline: PC_RPAsset (URP).

# Game Mechanics
## Core Gameplay Loop
- Exploration of a detailed urban environment centered around the historic Seoul Station.
## Controls and Input Methods
- Character movement (PlayerMovement.cs) and Camera control (CameraFollow.cs).

# UI
- Minimalist HUD (if any) to maintain immersion in the urban landscape.

# Key Asset & Context
- **Base**: `Old Seoul Station Assets` (Root object in scene).
- **Buildings**:
  - `Assets/01 Seoul Station/02 Buildings/Modern Glass Skyscraper/model.obj`
  - `Assets/01 Seoul Station/02 Buildings/Terracotta Skyscraper/model.obj`
  - `Assets/01 Seoul Station/02 Buildings/Modern Office Building/model.obj`
  - `Assets/01 Seoul Station/02 Buildings/Highrise Apartment/model.obj`
  - `Assets/01 Seoul Station/02 Buildings/GS25 Convenience Store/model.obj`
  - `Assets/01 Seoul Station/02 Buildings/Cafe Building/model.obj`
- **Infrastructure**:
  - `Assets/01 Seoul Station/04 Architectures/Elevated Pedestrian Walkway/model.obj` (Seoullo 7017)
  - `Assets/01 Seoul Station/01 Tiles/Black Textured Asphalt/model.obj` (Roads)
  - `Assets/01 Seoul Station/01 Tiles/Asphalt Crosswalk Stripes/model.obj` (Crosswalks)
  - `Assets/01 Seoul Station/04 Architectures/Modern Street Light/model.obj`
- **Traffic**:
  - `Assets/01 Seoul Station/06 Traffic/Blue City Bus/model.obj`
  - `Assets/01 Seoul Station/06 Traffic/Green City Bus/model.obj`
  - `Assets/01 Seoul Station/06 Traffic/Yellow Taxi/model.obj`
  - `Assets/01 Seoul Station/06 Traffic/White SUV/model.obj`
- **Nature**:
  - `Assets/01 Seoul Station/07 Nature/Rooted Green Tree/model.obj`
  - `Assets/01 Seoul Station/07 Nature/Green Rectangular Hedge/model.obj`

# Implementation Steps
## 1. Scene Baseline Correction
- **Action**: Adjust `Old Seoul Station Assets` and its children to Y=0.
- **File**: `Old_Seoul_Station.unity` (Scene modification).
- **Details**: Move the root object `Old Seoul Station Assets` from `(0, -6.5, 0)` to `(0, 0, 0)`. Adjust `Full floor` to match the new ground level.

## 2. Road and Sidewalk Layout
- **Action**: Create a main boulevard in front of the station.
- **Details**:
  - Place `Black Textured Asphalt` tiles to form a multi-lane road along the X-axis (in front of the station).
  - Add `Asphalt Crosswalk Stripes` at logical intersection points.
  - Use `Grey Paving Tiles` for sidewalks.

## 3. Elevated Pedestrian Walkway (Seoullo 7017)
- **Action**: Construct the elevated walkway spanning the roads.
- **Details**:
  - Use multiple segments of `Elevated Pedestrian Walkway`.
  - Position them at an elevated height (approx. Y=5-8) crossing the main road.
  - Add `Black Ornate Railing` along the edges of the walkway.
  - Place `Concrete Flower Planter` and `Green Shrub` on the walkway to simulate the real Seoullo 7017 style.

## 4. Building Placement (Skyline)
- **Action**: Place skyscrapers and office buildings to create the urban canyon.
- **Details**:
  - **Front/Sides**: Place `Modern Glass Skyscraper` and `Terracotta Skyscraper` to represent the iconic buildings near the station.
  - **Street Level**: Place `GS25 Convenience Store` and `Cafe Building` along the sidewalks for detail.
  - **Background**: Use `Highrise Apartment` and `Modern Office Building` to fill the distance.

## 5. Traffic and Street Details
- **Action**: Populate the scene with vehicles and street props.
- **Details**:
  - Place `Blue City Bus` and `Green City Bus` in designated bus lanes.
  - Scatter `Yellow Taxi` and `White SUV` along the roads.
  - Add `Modern Street Light` and `Traffic Light Pole` at intersections.
  - Place `Modern Trash Can` and `Wooden Park Bench` on sidewalks.

## 6. Greenery and Final Polish
- **Action**: Add natural elements and refine lighting.
- **Details**:
  - Line the streets and walkway with `Rooted Green Tree` and `Green Leaf Tree`.
  - Use `Green Rectangular Hedge` to define pedestrian zones.
  - Ensure `Global Volume` (Post-processing) enhances the modern city look.

# Verification & Testing
- **Visual Check**: Compare the resulting scene with the three provided perspective images (X, Z, -X).
- **Player Navigation**: Move the `Player` character around to ensure there are no collider issues and the scale feels correct.
- **Collision Test**: Verify that vehicles and buildings have appropriate colliders (MeshCollider or BoxCollider).
