# Bird Family · The Way Home

Lead the mother bird through the meadow to gather five chicks, then bring the flock back to the glowing nest. Finish in 22 seconds to meet the optional time goal. Take as long as you like to complete the flight.

## Play in Unity

Open this folder as a project in Unity **2021.1.17f1**. Open **Assets → Scenes → TheWayHome** and press **Play**. This is the default scene for new player builds. [Unity 2021.1.17f1 download](https://unity.com/releases/editor/whats-new/2021.1.17f1)

- Click or tap the meadow to fly to a point. On a keyboard, use WASD or the arrow keys to steer directly.
- Fly near each chick to recruit it. The objective changes when the fifth chick joins.
- Use the pause button or Esc to pause. Resume, restart, or replay from the menus.

## Show it on an iPhone or iPad

1. In Unity Hub, add **iOS Build Support** to the Unity editor. Open the project, go to **File → Build Settings**, select **iOS**, and click **Switch Platform**.
2. Choose **Bird Family → Export iOS Demo**. Unity writes an Xcode project to a new folder under **Builds/iOS**. The demo supports both landscape orientations and works without a camera or an AR session.
3. On a Mac with Xcode, open **Unity-iPhone.xcodeproj**. Select the **Unity-iPhone** target, open **Signing & Capabilities**, enable **Automatically manage signing**, and choose your team. If Xcode reports that the bundle identifier is unavailable, change it to one your team can use.
4. Connect and unlock the device, select it as the run destination in Xcode, and press **Run**. Accept the trust and Developer Mode prompts if your device requests them. Once installed, launch **BirdFamily** from the Home Screen. Tap **Take flight**, then tap the meadow to steer.

Windows can export the Xcode project. Signing and installing it on a device require Xcode on a Mac. See [Unity’s iOS build process](https://docs.unity3d.com/2021.1/Documentation/Manual/iphone-BuildProcess.html) and [Apple’s device setup](https://developer.apple.com/documentation/xcode/running-your-app-on-simulated-or-physical-devices).

The earlier camera playground is in **Assets/Scenes/SampleScene.unity**. To build it separately, enable it as the first scene and build through Unity's Build Settings with ARKit enabled. It needs an ARKit-capable device and camera permission.
