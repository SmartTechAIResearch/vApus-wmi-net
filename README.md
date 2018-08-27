A WMI agent written in C# (dotnet 4.5). Bypasses all remote WMI problems. Listens to TCP port 5556 (can be set in the file vApus-agent.properties).

Not derived from the vApus-agent base classes, since those are Java (and Java bridges to dotnet == evil), but similar to it and uses the same communication protocol. See the vApus-agent readme for a description of that.

The AssemblyFileVersion should be updated each time a new version is released.