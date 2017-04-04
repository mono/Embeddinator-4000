This is a small wrapper around the command-line `leaks` tool that:

* Sets a few malloc variables before launching the executable that's to be
  tested.
* Launches the executable, requests it to wait before exiting (this needs
  cooperation from the executable), and then launches `leaks` when the
  executable is at the end.

It also processes the output from `leaks` to make it more human readable.
