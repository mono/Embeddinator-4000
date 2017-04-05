# managed.dll

This assembly serve as the base to create tests for generated code. As such it should be agnostic to anything generated flavors, i.e. pure .net code.

However it's fine to add comments, either general ones or some specific to how the code would translate. In the later case just prefix the comment with the target, e.g. `// objc: my notes...`
