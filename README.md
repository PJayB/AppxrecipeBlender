AppxrecipeBlender
=================

http://twitter.com/PJayB

This tool splices in additional files for deployment into Windows Runtime packages. I needed much more control about the layout of my package, but Visual Studio insisted on using a flat layout. From what I could see on the Internet there wasn't any way to change this, so I wrote this.

This tool can give you: 
- More control over what files go into your packages and where they are on your disk.
- Where those files go inside the package. Subdirectory support!
 
Usage
-----

Run as a post build step. Use these flags to control the tool:

- `/D <name> <value>`: Defines a constant for replacement in the mapping file.
- `/recipefile`: The input .appxrecipe file.
- `/outfile`: The output .appxrecipe file (can be same as input).
- `/mappingfile`: The key -> value pairs of path mappings.

Defining a constant `OUTDIR` as `Foo!` will replace all instances of `%OUTDIR%` inside your mapping file with `Foo!`.

Mapping Files
-------------

Mapping rules are defined by  `(source) -> (target)`. `(target)` can be empty. Wildcards (* and ?) are supported.

**Example 1**: `..\..\some\other\file.txt -> \`

Deploys file.txt from that external folder to package root.


**Example 2**: `*.dll -> \subfolder`

Deploys all DLLs to a subfolder in the package.

**Example 3**: `%OUTDIR%\file??.txt -> \`

Looks in %OUTDIR% (defined with /D) and deploys fileXX.txt to package root.

Here's the one I use for my Quake 3: Arena Windows 8 port to Surface:

    ..\..\baseq3\autoexec.cfg                   -> baseq3   
    ..\..\baseq3\*.pk3                          -> baseq3   
    %OUTDIR%\*.dll                              ->    
    %OUTDIR%\..\%CONFIGURATION% Win8\*.cso      -> baseq3\hlsl

With the following command line:

    $(ProjectDir)AppxrecipeBlender /mapfile "$(ProjectDir)packagelist.txt" /recipefile "$(OutDir)$(ProjectName).build.appxrecipe" /outfile "$(OutDir)$(ProjectName).build.appxrecipe" /D CONFIGURATION "$(Configuration)" /D PLATFORM "$(Platform)" /D OUTDIR "$(OutDir)."
