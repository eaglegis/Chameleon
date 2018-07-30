using PowerArgs;

namespace Chameleon
{
    public class CmdArgs
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgShortcut("-input"), ArgDescription("Location of the .lyr file to be processed.")]
        [ArgRequired(PromptIfMissing = true)]
        [ArgExistingFile]
        [ArgRegex(@".*\.lyr", "Input file must be a .lyr file")]
        public string InputFile { get; set; }

        [ArgShortcut("-output"), ArgDescription("Location of the file system folder to save the processed file to.")]
        [ArgRequired(PromptIfMissing = true)]
        public string OutputFolder { get; set; }

        [ArgShortcut("-hex"), ArgDescription("Hex colour value to set the input .lyr file symbol to. Can omit the # if you want.")]
        [ArgRequired(PromptIfMissing = true)]
        public string Colour { get; set; }

        [ArgShortcut("-useInput"), ArgDescription("If true then the app will try to get the symbol type from the inpit filename. The file should only contain the symbol type e.g. point.lyr")]
        [ArgDefaultValue(true)]
        public bool GetSymbolTypeFromInputFileName { get; set; }

        [ArgShortcut("-saveUnique"), ArgDescription("If true then the app will get a unique filename for the output file and not overwrite any existing files.")]
        public bool SaveFileNameUnique { get; set; }

        [ArgShortcut("-wait"), ArgDescription("If true then the app will not exit until the user presses the 'Esc' key.")]
        public bool WaitToExit { get; set; }

        [ArgShortcut("-symbol"), ArgDescription("Symbol (geometry) type to set for the output layer. Default is point.")]
        [ArgDefaultValue("point")]
        public string SymbolType { get; set; }

        [ArgShortcut("-size"), ArgDescription("Size of the point symbol to use if symbol type is set as point. Default is 10.")]
        [ArgDefaultValue(10.0)]
        public double PointSize { get; set; }

        [ArgShortcut("-width"), ArgDescription("With of the polyline symbol to use if symbol type is set as polyline. or the width of the polygon outline if symbol type is set to polygon. Default is 1.")]
        [ArgDefaultValue(1.0)]
        public double LineWidth { get; set; }

        [ArgShortcut("-hexOutline"), ArgDescription("Hex colour value to set the polygon symbol outline to. If not set then the outline will be the same colour as the fill. Can omit the # if you want.")]
        public string OutlineColour { get; set; }
    }
}
