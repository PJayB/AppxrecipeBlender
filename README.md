AppxrecipeBlender
=================

http://twitter.com/PJayB

This tool splices in additional files for deployment into Windows Runtime packages.

This gives you: 
 1) More control over what files go into your packages.
 2) Where those files go. Subdirectory support!
 
Usage
-----

Run as a post build step. Use these flags to control the tool:

 `/D <name> <value>`: Defines a variable for replacement in the mapping file
 `/recipefile`: The input .appxrecipe file
 `/outfile`: The output .appxrecipe file (can be same as input)
 `/mappingfile`: The key -> value pairs of path mappings

Mapping Files
-------------

Mapping rules are defined by 

  `(source) -> (target)`
  
`(target)` can be empty. Wildcards (* and ?) are supported.

Example mapping file:

`..\..\some\other\file.txt ->                  ` (Example 1)
`*.dll                     -> subfolder        ` (Example 2)
`%OUTDIR%\file??.txt       ->                  ` (Example 3)

Explanation:
 (1) Deploys file.txt from that external folder to package root.
 (2) Deploys all DLLs to a subfolder in the package.
 (3) Looks in %OUTDIR% (defined with /D) and deploys fileXX.txt to package root.

