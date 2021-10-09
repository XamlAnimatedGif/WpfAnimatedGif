WPF Animated GIF
================

[![NuGet version](https://img.shields.io/nuget/v/WpfAnimatedGif.svg?logo=nuget)](https://www.nuget.org/packages/WpfAnimatedGif)
[![AppVeyor build](https://img.shields.io/appveyor/ci/thomaslevesque/wpfanimatedgif.svg?logo=appveyor&logoColor=cccccc)](https://ci.appveyor.com/project/thomaslevesque/wpfanimatedgif)

_Nuget package available here: [WpfAnimatedGif](https://nuget.org/packages/WpfAnimatedGif)._

A simple library to display animated GIF images in WPF, usable in XAML or in code.

It's very easy to use: in XAML, instead of setting the `Source` property, set the `AnimatedSource` attached property to the image you want:

```xml
<Window x:Class="WpfAnimatedGif.Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:gif="http://wpfanimatedgif.codeplex.com"
        Title="MainWindow" Height="350" Width="525">
    <Grid>
        <Image gif:ImageBehavior.AnimatedSource="Images/animated.gif" />
```

You can also specify the repeat behavior (the default is `0x`, which means it will use the repeat count from the GIF metadata):

```xml
        <Image gif:ImageBehavior.RepeatBehavior="3x"
               gif:ImageBehavior.AnimatedSource="Images/animated.gif" />
```

And of course you can also set the image in code:

```csharp
var image = new BitmapImage();
image.BeginInit();
image.UriSource = new Uri(fileName);
image.EndInit();
ImageBehavior.SetAnimatedSource(img, image);
```

See the [wiki](https://github.com/XamlAnimatedGif/WpfAnimatedGif/wiki) for more details on usage.

Features
--------

* Animates GIF images in a normal `Image` control; no need to use a specific control
* Takes actual frame duration into account
* Repeat behavior can be specified; if unspecified, the repeat count from the GIF metadata is used
* Notification when the animation completes, in case you need to do something after the animation
* Animation preview in design mode (must be enabled explicitly)
* Support for controlling the animation manually (pause/resume/seek)

How to build
------------

Run `build.cmd`.

Note: the library's version number is determined by [MinVer](https://github.com/adamralph/minver) based on Git history and tags. A consequence of this is that if you build the project outside a Git repository (e.g. if you just download sources), you'll get a version number of 0.0.0.0. So, in order to build with the correct version number, make sure you're in a Git clone of the project, and that your clone has the tags from the upstream project (`git fetch upstream --tags`, assuming your remote for the upstream project is named `upstream`).
