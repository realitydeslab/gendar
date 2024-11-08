# Image Tracking Relocalization

## Overview

In certain AR projects, it's essential to transform the entire virtual coordinate system to align virtual content with the physical environment or synchronize multiple devices in multiplayer settings. This Unity plugin enhances ARFoundation's image tracking, enabling device coordinate system relocalization through tracked image poses. A notable challenge with ARFoundation's image tracking is its inconsistent stability; relying on single instances often leads to significant deviations. Our plugin addresses this by stablizing image tracking through a sequence of consecutively tracked image poses, improving the accuracy of the final outcome.

The plugin provides two relocalization methods, which are external marker-based and dynamically rendered marker-based approaches, making it suitable for both single-player to multiplayer experiences.

## How To Install

<img width="228" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/df9e812b-2334-40e2-8d40-13d2b2b87cc9">

To install this package, go to Unity Package Manager, click the + button in the top-left corner and choose "Install package from git URL". Then input the following git URL:

```
https://github.com/holoi/com.holoi.xr.image-tracking-relocalization.git
```

This package requires two git URL-based dependency packages, the [HoloKit Unity SDK package](https://github.com/holoi/holokit-unity-sdk) and the [Netcode MultipeerConnectivity transport package](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.multipeer-connectivity). The HoloKit Unity SDK is essential for accessing the physical parameters of iPhone models, and the Netcode MultipeerConnectivity transport is necessary for connecting nearby iOS devices.

To install these dependencies, please use the following two git URLs with the same approach explained above:

```
https://github.com/holoi/holokit-unity-sdk.git
```
```
https://github.com/Unity-Technologies/multiplayer-community-contributions.git?path=/Transports/com.community.netcode.transport.multipeer-connectivity
```

This manual setup is necessary because I don't know how to integrate git URL-based dependency packages directly into the `package.json` file. If you know how to do it, please contact me at `yuchenz27@outlook.com`. Thank you!
 
## Project Environment

Please be aware that these boilerplates are designed exclusively for iOS devices.

We have successfully tested them with the following software versions:

- Unity 2023.2.2f1
- Xcode 15.1 beta 3

In theory, other versions of Unity and Xcode that are close to these should also be compatible. However, if you encounter any issues during the build process, feel free to raise an issue in the repository.

## External Marker Relocalization

This approach is suited for both single-player and multiplayer projects. It functions by utilizing an external marker image, which, when scanned by one or several devices, allows them to relocalize their coordinate origins to the image's location. This method is straightforward and easy to implement. However, it's important to note that external markers might not always be available or suitable for every scenario.

### How To Use External Marker To Relocalize

To facilitate a smooth start, we've included an "External Marker Relocalization" sample in the package. Importing this sample into your project can help you quickly grasp the setup and usage of this feature.

For relocalizing the coordinate system with an external marker, the `ImageTrackingStablizer` and `WorldTransformResetter` scripts are required.

<img width="368" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/09892e95-64af-4015-a5d1-28fbaeb1ec8f">

The `ImageTrackingStablizer` script calculates the standard deviation of poses from multiple consecutively tracked images. When this deviation falls below a predefined threshold, indicating stable tracking, it captures the pose of the last tracked image as the final pose. Subsequently, it triggers a Unity event `OnTrackedImagePoseStablized`, passing the image's position and rotation as parameters.

The `OnTrackedImagePoseStablized` Unity event is received by the `WorldTransformResetter` script. To facilitate easy transformation of the entire coordinate system, all game objects are nested under a root object named "World Transform". Upon detecting a stable image pose, this root object is moved to the image location, causing all child objects to follow suit. While alternative methods for coordinate system relocalization are possible and we did reset the coordinate system origin in "Dynamically Rendered Marker Relocalization" sample, this straightforward approach offers a viable option.

<img width="423" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/e257d028-e2d9-4953-986e-844271fef596">

If you choose not to use a root object for relocalizing all game objects, you can omit the `WorldTransformResetter` script and apply your own method. Simply handle the `OnTrackedImagePoseStablized` event from the `ImageTrackingStablizer` in your custom script.

The accompanying GIF demonstrates the external marker relocalization process. Initially, a cube marked with x, y, and z axes is positioned at (0, 0, 0). Once the image tracking algorithm identifies a stable pose of the QR code, the coordinate system relocates to the image's position, resulting in the cube moving to the image location.

![ezgif-1-d80370e909](https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/ba8d577c-5b87-4244-a5a5-dce3cffe7c94)

## Dynamically Renderer Marker Relocalization

In many cases, an external marker might not be readily available, as it's impractical to expect users to carry such markers with them at all times. Addressing this, our plugin offers a solution specifically designed for multiplayer AR experiences. In this setup, the host device dynamically renders a marker image on its screen, which client devices then scan to relocalize their coordinate systems. This method, while more sophisticated due to the involvement of network communications, offers a practical solution for multiplayer scenarios where external markers are not feasible.

![ezgif-3-1cbea9d112](https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/5dde4cce-de5e-4efc-af97-383f2b182058)

### How Dynamically Rendered Marker Relocalization Works

This approach focuses on stable tracking of a marker displayed on the host device's screen. It synchronizes the coordinate origins of all client devices with the host device's origin, utilizing a series of tracked image poses and communication between the host and client devices.

The synchronization process comprises two phases. The first phase calculate the timestamp offset between the host and client devices. In the second phase, the relative translation and rotation from the client to the host device are determined.

#### Timestamp Synchronization

The goal of this phase is to determine the timestamp offset between a client and the host devices. This is crucial for aligning each image pose tracked by the client device with its corresponding pose in the host's coordinate system.

In this phase, the client device continuously sends its current local timestamp to the host device. Upon receiving a timestamp from a client, the host replies with its local timestamp at that time. The client then calculates an instance of the timestamp offset using the formula:

```
timestampOffset = hostTimestamp + (currentClientTimestamp - previousClientTimestamp) / 2 - currentClientTimestamp
```

The client accumulates these calculated offsets in a queue. Once the queue size surpasses a set threshold, the client computes the standard deviation of the offsets. If this deviation is below a predefined value, indicating stability, the client then averages all queue elements to determine the final timestamp offset.

#### Coordinate System Synchronization

After calculating the local timestamp offset relative to the host, a client begins tracking the target marker displayed on the host device's screen. Each time the client detects a tracked image pose, it requests the host to provide the corresponding pose with the nearest timestamp. This forms a pair of image poses: one from the client's local coordinate system and the corresponding one from the host's system. These pairs are queued on the client device.

When the queue size of these image pose pairs exceeds a certain threshold, the least squares method is employed to compute the synchronization result. This result includes the relative translation and rotation from the client's origin to the host's origin. The client continually calculates and queues these synchronization resultss. Upon reaching another predefined threshold in the synchronization result queue, the standard deviation of the queue elements is calculated. If this deviation is sufficiently small, indicating stability, the latest element is taken as the final synchronization result. This final result is then used to reset the client device's coordinate system origin.

### How To Use Dynamically Rendered Marker To Relocalize

The package contains the "Dynamically Rendered Marker Relocalization" sample, a multiplayer AR project demo. For building your own multiplayer AR project, we suggest starting with this [multiplayer AR boilerplate](https://github.com/holoi/holokit-multiplayer-ar-boilerplates). It leverages this package and can simplify your project development.

<img width="380" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/96673df5-9d0e-427a-8ffd-e45598da7a45">

The scene incorporates several network components. For those unfamiliar with Unity's Netcode for GameObjects and multiplayer programming, understanding the entire code base my be challenging. We recommend consulting the [Unity Netcode for GameObjects documentation](https://docs-multiplayer.unity3d.com/netcode/current/about/) for more information.

The key script in this process is `NetworkImageTrackingStablizer`, which aims to stably track the marker image on the host device's screen.

<img width="854" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/33dc9773-b2ae-4dff-b283-d24fc483bc45">

Given the complexity of the two phases discussed earlier, this script includes several calibrated parameters on both the host and client sides. We advise against modifying these parameters unless you thoroughly understand the process.

The script operates differently on host and client sides, and that is why we use a partial class to seperate the script. On the host, it works alongside the `MarkerRenderer` script to create the marker image and calculate offsets related to the physical dimensions of iPhones. On the client side, it handles calculating the timestamp offset and the synchronization of the coordinate system. It triggers Unity events like `OnTimestampSynced` and `OnPoseSynced` to indicate successful timestamp and origin synchronization.

<img width="848" alt="image" src="https://github.com/holoi/com.holoi.xr.image-tracking-relocalization/assets/44870300/57347153-4100-461c-a995-fd41c6757f0b">

It's crucial to note that after the client resets its origin, the user must visually validate the synchronization result. The client renders an alignment marker, which is a visual frame around the host device. If this marker accurately represents the host's real-time physical location, the user should confirm the synchronization result, triggering the `OnAlignmentMarkerAccepted` event. If the alignment is off, `OnAlignmentMarkerDenied` is invoked, and the client attempts to resynchronize the coordinate system for a more accurate result.

While the synchronization result starts accurately after the user's visual validation, please be aware that this method doesn't ensure lasting precision. Accuracy may decline during prolonged sessions or with substantial device movement. In case of significant deviation, realign the coordinates by rescanning the marker image.
