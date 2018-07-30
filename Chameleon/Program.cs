using ESRI.ArcGIS.esriSystem;
using System;
using ColoredConsole;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using PowerArgs;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Chameleon
{
    class Program
    {
        const string logo = @"
                      _       _._
               _,,-''' ''-,_ }'._''.,_.=._
            ,-'      _ _    '        (  @)'-,
          ,'  _..==;;::_::'-     __..----'''}
         :  .'::_;==''       ,'',: : : '' '}
        }  '::-'            /   },: : : :_,'
       :  :'     _..,,_    '., '._-,,,--\'    _
      :  ;   .-'       :      '-, ';,__\.\_.-'
     {   '  :    _,,,   :__,,--::',,}___}^}_.-'
     }        _,'__''',  ;_.-''_.-'
    :      ,':-''  ';, ;  ;_..-'
_.-' }    ,',' ,''',  : ^^
_.-''{    { ; ; ,', '  :
      }   } :  ;_,' ;  }
       {   ',',___,'   '
        ',           ,'
          '-,,__,,-'
";

        static LicenseInitializer m_AOLicenseInitializer = new LicenseInitializer();

        [STAThread()]
        static void Main(string[] args)
        {
            //ESRI License Initializer generated code.
            if (!m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[]
            {
                esriLicenseProductCode.esriLicenseProductCodeBasic,
                esriLicenseProductCode.esriLicenseProductCodeStandard,
                esriLicenseProductCode.esriLicenseProductCodeAdvanced,
                esriLicenseProductCode.esriLicenseProductCodeArcServer
            },
            new esriLicenseExtensionCode[] { }))
            {
                ColorConsole.WriteLine(m_AOLicenseInitializer.LicenseMessage().DarkRed());
                ColorConsole.WriteLine("This application could not initialize with the correct ArcGIS license and will shutdown.".Red());
                m_AOLicenseInitializer.ShutdownApplication();
                return;
            }

            string executingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                ?? Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath); // need this for testing

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(Path.Combine(executingPath, @"Log-{Date}.txt"))
                .CreateLogger();

            ColorConsole.WriteLine(logo.DarkGreen());
            Console.WriteLine();
            Log.Information("[Starting Chameleon]");
            Console.WriteLine();

            var cmdArgs = new CmdArgs();
            try
            {
                cmdArgs = Args.Parse<CmdArgs>(args);
                if (cmdArgs != null)
                {
                    RunJob(cmdArgs);
                }
            }
            catch (ArgException ex)
            {
                Log.Error("Unable to parse cmd args. {Exception}", ex);
            }
            catch (Exception ex)
            {
                Log.Error("Error processing layer. {Exception}", ex);
            }

            //ESRI License Initializer generated code.
            //Do not make any call to ArcObjects after ShutDownApplication()
            m_AOLicenseInitializer.ShutdownApplication();

            Console.WriteLine();
            Log.Information("[Finished Chameleon]");
            Console.WriteLine();

            if (cmdArgs != null && cmdArgs.WaitToExit)
            {
                ColorConsole.WriteLine("[Press".Gray(), " Esc ".Red(), "to exit]".Gray());
                Console.WriteLine();

                var e = Console.ReadKey();
                while (e.Key != ConsoleKey.Escape)
                    e = Console.ReadKey();
            }
        }

        static void RunJob(CmdArgs args)
        {
            Log.Information("Processing file {LayerFile}", args.InputFile);
            var inputFile = new FileInfo(args.InputFile);

            ILayerFile inputLayer = new LayerFileClass();
            inputLayer.Open(inputFile.FullName);

            IGeoFeatureLayer geoFeatureLayer = inputLayer.Layer as IGeoFeatureLayer;
            //var featureLayer = inputLayer.Layer as FeatureLayer;
            //IFeatureClass featureClass = featureLayer.FeatureClass;     
            //var renderer = geoFeatureLayer.Renderer as ISimpleRenderer;     

            var esriRgb = CreateRGBColor(args.Colour);

            ISimpleRenderer simpleRenderer = new SimpleRendererClass();

            var symbolType = args.SymbolType.ToLowerInvariant();
            if (args.GetSymbolTypeFromInputFileName)
            {
                var tempSymbolType = inputFile.Name.Replace(inputFile.Extension, "").ToLowerInvariant();
                if (string.Equals(tempSymbolType, "point", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tempSymbolType, "polyline", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tempSymbolType, "line", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tempSymbolType, "polygon", StringComparison.OrdinalIgnoreCase))
                {
                    symbolType = tempSymbolType;
                }
            }

            switch (symbolType)
            {
                case "point":
                    IMarkerSymbol pointSymbol = new SimpleMarkerSymbolClass();
                    pointSymbol.Size = args.PointSize;
                    pointSymbol.Color = esriRgb;
                    simpleRenderer.Symbol = (ISymbol)pointSymbol;
                    break;

                case "polyline":
                case "line":
                    ILineSymbol lineSymbol = new SimpleLineSymbolClass();
                    lineSymbol.Width = args.LineWidth;
                    lineSymbol.Color = esriRgb;
                    simpleRenderer.Symbol = (ISymbol)lineSymbol;
                    break;

                case "polygon":
                    ILineSymbol outlineSymbol = new SimpleLineSymbolClass();
                    outlineSymbol.Width = args.LineWidth;
                    outlineSymbol.Color = CreateRGBColor(args.OutlineColour) ?? esriRgb;
                    ISimpleFillSymbol fillSymbol = new SimpleFillSymbolClass();
                    fillSymbol.Outline = outlineSymbol;
                    fillSymbol.Color = esriRgb;
                    simpleRenderer.Symbol = (ISymbol)fillSymbol;
                    break;

                default:
                    inputLayer.Close();
                    Log.Warning("Unrecognized symbol type {SymbolType}. No symbol set for the layer renderer. Valid symbol types are point, polyline, line or polygon.", args.SymbolType);
                    return;
            }

            geoFeatureLayer.Renderer = (IFeatureRenderer)simpleRenderer;

            var outputFileLocation = args.SaveFileNameUnique ? GetUniqueFilename(Path.Combine(args.OutputFolder, inputFile.Name)) : Path.Combine(args.OutputFolder, inputFile.Name);
            var outputFile = new FileInfo(outputFileLocation);
            Log.Information("Saving processed layer file to {OutputFile}", outputFile.FullName);

            SaveToLayerFile(outputFile.FullName, inputLayer.Layer);

            inputLayer.Close();
        }

        static string GetUniqueFilename(string fullPath)
        {
            int count = 1;
            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;

            while (File.Exists(newFullPath))
            {
                Log.Information("{FilePath} already exists, getting a new file name.", newFullPath);
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                newFullPath = Path.Combine(path, tempFileName + extension);
            }

            return newFullPath;
        }

        ///<summary>Write a Layer to a file on disk.</summary>
        ///  
        ///<param name="layerFilePath">A System.String that is the path and filename for the layer file to be created. Example: "C:\temp\cities.lyr"</param>
        ///<param name="layer">An ILayer interface.</param>
        ///   
        ///<remarks></remarks>
        public static void SaveToLayerFile(string layerFilePath, ILayer layer)
        {
            if (layer == null)
            {
                Log.Warning("No valid layer file was found. Save not completed.");
                return;
            }
            //create a new LayerFile instance
            ESRI.ArcGIS.Carto.ILayerFile layerFile = new LayerFileClass();

            //make sure that the layer file name is valid
            if (Path.GetExtension(layerFilePath) != ".lyr")
                return;
            if (layerFile.get_IsPresent(layerFilePath))
                File.Delete(layerFilePath);

            var fileInfo = new FileInfo(layerFilePath);
            if (!fileInfo.Directory.Exists)
            {
                Log.Information("Creating output directory {OutputDirectory}.", fileInfo.Directory.FullName);
                fileInfo.Directory.Create();
            }

            //create a new layer file
            layerFile.New(layerFilePath);

            //attach the layer file with the actual layer
            layerFile.ReplaceContents(layer);

            //save the layer file
            layerFile.Save();

            layerFile.Close();

            Log.Information("Layer saved.");

            // save output path so that the calling process can get it - useful when unique file name for output
            string tempFile = Path.Combine(Path.GetTempPath(), "Chameleon.txt");
            using (StreamWriter sw = new StreamWriter(tempFile))
            {
                sw.WriteLine(layerFilePath);
            }
        }

        ///<summary>Generate an RgbColor by specifying the amount of Red, Green and Blue.</summary>
        /// 
        ///<param name="myRed">A byte (0 to 255) used to represent the Red color. Example: 0</param>
        ///<param name="myGreen">A byte (0 to 255) used to represent the Green color. Example: 255</param>
        ///<param name="myBlue">A byte (0 to 255) used to represent the Blue color. Example: 123</param>
        ///  
        ///<returns>An IRgbColor interface</returns>
        ///  
        ///<remarks></remarks>
        public static IRgbColor CreateRGBColor(Byte myRed, Byte myGreen, Byte myBlue)
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = myRed;
            rgbColor.Green = myGreen;
            rgbColor.Blue = myBlue;
            rgbColor.UseWindowsDithering = true;
            return rgbColor;
        }

        public static IRgbColor CreateRGBColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
            {
                return null;
            }

            var hex = "#" + hexColor.Replace("#", "");
            var rgb = System.Drawing.ColorTranslator.FromHtml(hex);

            Log.Information("Setting symbol colour to {HexColour}, {RgbColour}", hex, rgb);

            return CreateRGBColor(rgb.R, rgb.G, rgb.B);
        }
    }
}
