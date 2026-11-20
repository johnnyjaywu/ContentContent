# Camera Playground Sample

Drop-in sample for `com.contentcontent.camera` using Unity's New Input System (`com.unity.inputsystem`).

## Usage

1. Select **ContentContent - Camera** in **Window -> Package Manager** (with `com.contentcontent.core` installed) and click **Download** under **Downloadable Samples**.
2. Attach `CameraPlaygroundSample` to an empty GameObject in a scene with a Main Camera and enter Play Mode.
3. Move with `A`/`D` (or Left/Right arrows) to observe delayed directional look-ahead, stop and hold `W`/`S` to trigger grounded vertical peek, press `B` to toggle a priority Boss Arena lock + zoom zone, and press `R` to trigger an instant snap cut.
4. For Cinemachine 3 setups, install `com.unity.cinemachine` (`3.0+`) and pair `CameraDirectorBehaviour` with `CinemachineCameraBridge`.
