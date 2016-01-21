# watch
Scrappy little path watcher for Windows

# Usage
```
cd ProjectDirectory
watch make
```
Will cause `make` to be called when any of the source files in your directory change. File extensions are currently filtered for C++ projects, but this is easy to change (see line [54](https://github.com/GeorgeHahn/watch/blob/master/Program.cs#L54)).
