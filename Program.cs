using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AppxrecipeBlender
{
    class Program
    {
        static List<string> LoadFile(string file)
        {
            List<string> l = new List<string>();
            using (StreamReader r = new StreamReader(file))
            {
                while (!r.EndOfStream)
                {
                    l.Add(r.ReadLine());
                }
            }
            return l;
        }

        class EnvironmentVars
        {
            public EnvironmentVars()
            {
                _vars = new Dictionary<string, string>();
            }

            public void Add(string name, string value)
            {
                _vars['%' + name + '%'] = value;
            }

            public string Replace(string s)
            {
                foreach (var v in _vars)
                {
                    s = s.Replace(v.Key, v.Value);
                }
                return s;
            }

            private Dictionary<string, string> _vars;
        };

        class FileMapping
        {
            public struct MappingDef
            {
                public string SourcePath;
                public string Pattern;
                public string TargetPath;
            };

            static private MappingDef MakeMapping(string pattern, string target)
            {
                string path = "";

                // If the pattern has a wildcard, split it from the rest of the path
                int wildCardPos = pattern.IndexOfAny(new char[] { '*', '?' });
                if (wildCardPos != -1)
                {
                    // Find the last delimiter before the wildcard
                    int delimPos = pattern.LastIndexOfAny(new char[] { '/', '\\' }, wildCardPos);
                    if (delimPos != -1)
                    {
                        path = pattern.Substring(0, delimPos);
                        pattern = pattern.Substring(delimPos + 1);
                    }
                }
                else
                {
                    path = Path.GetDirectoryName(pattern);
                    pattern = Path.GetFileName(pattern);
                }

                MappingDef def = new MappingDef();
                def.Pattern = pattern;
                def.SourcePath = Path.GetFullPath(path);
                def.TargetPath = target;
                return def;
            }
            
            public FileMapping(string file, EnvironmentVars vars)
            {
                _mappings = new List<MappingDef>();

                List<string> defs = LoadFile(file);
                foreach (string def in defs)
                {
                    string trimmed = def.Trim();
                    if (trimmed.Length == 0)
                        continue;
                    if (!trimmed.Contains("->"))
                        throw new Exception("Expected '->': " + def);
                        

                    // Split it up by '->'
                    string[] tokens = trimmed.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 1)
                        throw new Exception("Syntax error for definition: " + def);

                    // Replace %variables% with values
                    string pattern = vars.Replace(tokens[0].Trim());
                    string target = "";

                    if (tokens.Length > 1)
                    {
                        string t = tokens[1].Trim();
                        if (t.Length > 0)
                        {
                            target = vars.Replace(tokens[1].Trim() + '\\');
                        }
                    }
                    
                    _mappings.Add(MakeMapping(pattern, target));
                }
            }

            public IEnumerable<MappingDef> Mappings
            {
                get { return _mappings; }
            }

            private List<MappingDef> _mappings;
        };

        static List<Tuple<string, string>> FindIngredients(string searchPath, string pattern, string destination)
        {
            List<Tuple<string, string>> list = new List<Tuple<string, string>>();

            var files = Directory.EnumerateFiles(searchPath, pattern);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string target = destination + name;
                Console.WriteLine(String.Format("{0} -> {1}", file, target));
                if ( !File.Exists(file) )
                    Console.WriteLine(String.Format("Warning: '{0}' doesn't appear to exist.", file));

                list.Add(new Tuple<string, string>(file, target));
            }

            return list;
        }

        static void SpliceRecipe(string recipeFile, string outFile, string xml)
        {
            List<string> lines = LoadFile(recipeFile);

            int insertionPoint = -1;
            for (int i = 0; i < lines.Count; ++i)
            {
                if (lines[i].Contains("</AppxPackagedFile>"))
                    insertionPoint = i;
            }

            // If there was nowhere to insert the file... well... bad recipe file?
            if (insertionPoint == -1)
                throw new Exception("Couldn't determine where to insert the ingredients. Bad recipe file?");

            // Open the file for writing
            using (StreamWriter w = new StreamWriter(outFile))
            {
                for (int i = 0; i <= insertionPoint; ++i)
                    w.WriteLine(lines[i]);

                w.Write(xml);

                // Finish writing the remaining lines
                for (int i = insertionPoint + 1; i < lines.Count; ++i)
                    w.WriteLine(lines[i]);
            }
        }

        static void Help()
        {
            Console.WriteLine("Splices in additional files for deployment into Windows Runtime packages.");
            Console.WriteLine(" /D <name> <value>: Defines a variable for replacement in the mapping file");
            Console.WriteLine(" /recipefile: The input .appxrecipe file");
            Console.WriteLine(" /outfile: The output .appxrecipe file (can be same as input)");
            Console.WriteLine(" /mappingfile: The key -> value pairs of path mappings");
            Console.WriteLine("");
            Console.WriteLine("Mapping rules are defined by (source) -> (target).");
            Console.WriteLine("(Target) can be empty.");
            Console.WriteLine("Wildcards (* and ?) are supported.");
            Console.WriteLine("");
            Console.WriteLine("Example mapping file:");
            Console.WriteLine("");
            Console.WriteLine(@"..\..\some\other\file.txt ->                  (Example 1)");
            Console.WriteLine(@"*.dll                     -> subfolder        (Example 2)");
            Console.WriteLine(@"%OUTDIR%\file??.txt       ->                  (Example 3)");
            Console.WriteLine("");
            Console.WriteLine("Explanation:");
            Console.WriteLine(" (1) Deploys file.txt from that external folder to package root.");
            Console.WriteLine(" (2) Deploys all DLLs to a subfolder in the package.");
            Console.WriteLine(" (3) Looks in %OUTDIR% (defined with /D) and deploys fileXX.txt to package root.");
        }

        static void SafeMain(string[] args)
        {
            string mappingFile = null;
            string recipeFile = null;
            string outFile = null;
            EnvironmentVars vars = new EnvironmentVars();

            // Parse command line
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].Length == 0)
                    continue;

                if (args[i][0] != '-' && args[i][0] != '/')
                    throw new Exception(String.Format("Unrecognized switch: '{0}'", args[i]));

                string sw = args[i].Substring(1).ToLower();

                switch (sw)
                {
                    case "?":
                        Help();
                        return;
                    case "d":
                        if (i >= args.Length - 2)
                            throw new Exception("Expected: /D NAME value");
                        vars.Add(args[++i], args[++i]);
                        break;
                    case "mapfile":
                        if (i >= args.Length - 1)
                            throw new Exception("Expected: mapfile value");
                        mappingFile = args[++i];
                        break;
                    case "recipefile":
                        if (i >= args.Length - 1)
                            throw new Exception("Expected: recipefile value");
                        recipeFile = args[++i];
                        break;
                    case "outfile":
                        if (i >= args.Length - 1)
                            throw new Exception("Expected: outfile value");
                        outFile = args[++i];
                        break;
                    default:
                        throw new Exception(String.Format("Unrecognized switch: '{0}'", args[i]));
                }
            }

            if (mappingFile == null)
                throw new Exception("Expected: /mapfile switch.");
            if (recipeFile == null)
                throw new Exception("Expected: /recipefile switch.");
            if (outFile == null)
                throw new Exception("Expected: /outfile switch.");

            Console.WriteLine("Mapping file: " + mappingFile);
            Console.WriteLine("Recipe file: " + recipeFile);
            Console.WriteLine("Output file: " + outFile);

            FileMapping mappings = new FileMapping(mappingFile, vars);

            List<Tuple<string, string>> ingredients = new List<Tuple<string, string>>();

            foreach (var mapping in mappings.Mappings)
            {
                ingredients.AddRange(FindIngredients(mapping.SourcePath, mapping.Pattern, mapping.TargetPath));
            }

            // Now generate the XML that we're going to splice into our mapping file
            StringBuilder xml = new StringBuilder();
            foreach (var ingredient in ingredients)
            {
                xml.AppendLine(String.Format("    <AppxPackagedFile Include=\"{0}\">", ingredient.Item1));
                xml.AppendLine(String.Format("      <PackagePath>{0}</PackagePath>", ingredient.Item2));
                xml.AppendLine("    </AppxPackagedFile>");
            }

            // Splice it in
            SpliceRecipe(recipeFile, outFile, xml.ToString());
        }

        static void Main(string[] args)
        {
            try
            {
                SafeMain(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
