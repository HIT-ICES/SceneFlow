# SceneFlow
This repository contains most of the resources for reproducing SceneFlow.


## File Structure
- `src`: Source code for SceneFlow.
  - `SceneFlowTools`: Core library for SceneFlow, serving as a Unity package.
  - `dev_service`: Stand-alone service for SceneFlow, providing various preprocessing functions during development.
- `apps`: Benchmark applications.
  All applications are built using Unity 2022.3.53, requires Crossport, an external framework. A vendored copy is provided under `third_party/Crossport` for reproducibility.
  - `Classroom`: Classroom app. Requires [EmeraldSquare v4.1](https://developer.nvidia.com/orca/nvidia-emerald-square), and [The-Virtual-Classroom](https://github.com/Arduino-Projects/The-Virtual-Classroom).
  - `Apartment`: Apartment app. Requires [Apartment Kit](https://assetstore.unity.com/packages/3d/environments/apartment-kit-124055).
- `third_party`: Third-party code and resources used by the project. Some of the code is modified for better integration with our project, and the original code can be found in the origional repositories.



If you have any trouble, feel free to contact me.

