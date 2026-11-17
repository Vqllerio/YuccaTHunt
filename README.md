# Yucca's Memory: The Fifth Floor

An Android AR treasure hunt built with Unity and Vuforia for the Informatics lounge at Universitas Ciputra Makassar. Scan the entrance poster to meet Yucca, the floor's memory-keeper, then find and scan five real objects in the lounge to restore its memories. Play solo or race other players online.

## Download

Get the latest APK from the [Releases page](../../releases/latest). On Android, allow "Install unknown apps" for your browser when asked.

## Building from source

- Unity 6000.6.5f1 with Android Build Support
- Vuforia Engine 11.4.4: the package file is too large for GitHub, so download `com.ptc.vuforia.engine-11.4.4.tgz` from the Vuforia Developer Portal and put it in the `Packages/` folder before opening the project

The Vuforia licence key is not included in this repository. After opening the project, open `Assets/Resources/VuforiaConfiguration` (Window > Vuforia Configuration) and paste your own key from the Vuforia Developer Portal.

The online race uses Supabase. Its project URL and publishable key are in `Assets/Scripts/YuccaBackendConfig.cs`; leave them empty to play in solo mode with a local leaderboard.

## Credits

- Yucca mascot artwork: Universitas Ciputra Makassar, used with permission
- Fonts: Space Grotesk (SIL Open Font License 1.1)
- Scan sound: freesound.org
