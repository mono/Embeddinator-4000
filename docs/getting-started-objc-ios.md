# Getting started with iOS

This is the getting started page for iOS.

*Working for tvOS or watchOS is very similar to iOS. You should start with the iOS instructions and then apply them to your platform of choice.*

## Requirements

In addition to the requirements from our [Getting started with Objective-C](getting-started-objective-c.md) guide you'll also need:

* [Xamarin.iOS 10.11+](https://jenkins.mono-project.com/view/Xamarin.MaciOS/job/xamarin-macios-builds-master/) from our _master_ branch.

## Hello world

First let's build a simple hello world example in C#.

### Create C# sample

Open Visual Studio for Mac, create a new iOS Class Library project, name it `hello-from-csharp`, and save it to `~/Projects/hello-from-csharp`.

Replace the code in the `MyClass.cs` file with the following snippet:

```csharp
using UIKit;
public class MyUIView : UITextView
{
	public MyUIView ()
	{
		Text = "Hello from C#";
	}
}
```

Build the project, the resulting assembly will be saved as `~/Projects/hello-from-csharp/hello-from-csharp/bin/Debug/hello-from-csharp.dll`.

### Bind the managed assembly

Run the embeddinator to create a native framework for the managed assembly:

```shell
cd ~/Projects/hello-from-csharp
objcgen ~/Projects/hello-from-csharp/hello-from-csharp/bin/Debug/hello-from-csharp.dll --target=framework --platform=iOS --outdir=output -c --debug
```

The framework will be placed in `~/Projects/hello-from-csharp/output/hello-from-csharp.framework`.

### Use the generated output in an Xcode project

Open Xcode and create a new iOS Single View Application, name it `hello-from-csharp` and select the **Objective-C** language.

Open the `~/Projects/hello-from-csharp/output` directory in Finder, select `hello-from-csharp.framework`, drag it to the Xcode project and drop it just above the `hello-from-csharp` folder in the project.

![Drag and drop framework](hello-from-csharp-ios-drag-drop-framework.png)

Make sure `Copy items if needed` is checked in the dialog that pops up, and click `Finish`.

![Copy items if needed](hello-from-csharp-ios-copy-items-if-needed.png)

Select the `hello-from-csharp` project and navigate to the `hello-from-csharp` target's **General tab**. In the **Embedded Binary** section, add `hello-from-csharp.framework`.

![Embedded binaries](hello-from-csharp-ios-embedded-binaries.png)

Open ViewController.m, and replace the contents with:

```objective-c
#import "ViewController.h"
#include "hello-from-csharp/hello-from-csharp.h"

@interface ViewController ()
@end

@implementation ViewController
- (void)viewDidLoad {
    [super viewDidLoad];

    MyUIView *view = [[MyUIView alloc] init];
    view.frame = CGRectMake(0, 200, 200, 200);
    [self.view addSubview: view];
}
@end
```

Finally run the Xcode project, and something like this will show up:

![Hello from C# sample running in the simulator](hello-from-csharp-ios.png)
