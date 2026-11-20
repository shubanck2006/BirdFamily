# Bird Family · The Way Home

Guide a mother bird across a low-poly meadow, gather five chicks, and bring the flock back to the glowing nest. The HUD tracks the current objective and your flight time. Finish within 22 seconds to meet the optional time goal.

## Download and play

1. Install **Unity Hub** and **Unity 2021.1.17f1**. [Get this editor version from Unity](https://unity.com/releases/editor/whats-new/2021.1.17f1).
2. Select **Code → Download ZIP**, then extract the ZIP. [Download project ZIP](https://github.com/shubanck2006/BirdFamily/archive/refs/heads/main.zip)
3. In Unity Hub, select **Projects → Add → Add project from disk** and choose the extracted folder that contains `Assets`, `Packages`, and `ProjectSettings`. Open the project and let Unity finish importing it.
4. In Unity’s **Project** panel, open **Assets → Scenes → TheWayHome**. Press **Play**, then select **Take flight**.

Click or tap a point in the meadow to fly there. On a keyboard, use **WASD** or **the arrow keys** to steer directly, **Space** to start, and **Esc** to pause or resume.

To clone the project with Git, run the following command and add the resulting `BirdFamily` folder to Unity Hub.

```sh
git clone https://github.com/shubanck2006/BirdFamily.git
```

## Make a desktop build

Open **File → Build Settings**, choose **PC, Mac & Linux Standalone**, and select your target operating system. Confirm that `Assets/Scenes/TheWayHome.unity` is enabled, then choose **Build** and a folder under `Builds`. Launch the app Unity creates there. Keep the generated files together when moving or sharing the build.

## Run on an iPhone or iPad

1. Add **iOS Build Support** for this Unity editor in Unity Hub. In Unity, open **File → Build Settings**, select **iOS**, and click **Switch Platform**.
2. Choose **Bird Family → Export iOS Demo**. The exported Xcode project appears under `Builds/iOS`.
3. On a Mac, open `Unity-iPhone.xcodeproj` in Xcode. Choose your signing team, select your connected iPhone or iPad, and press **Run**. Launch **BirdFamily** on the device and tap the meadow to steer.

[Detailed device signing steps and the original AR scene](DEMO.md)
