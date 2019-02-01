namespace Fake.StaticGen.Sass

open Fake.StaticGen

open SharpScss

/// See also: http://sass-lang.com/documentation/file.SASS_REFERENCE.html#output_style
type ScssOutputStyle =
    /// CSS rules are indented based on the original nesting
    | Nested
    /// CSS rules are not indented, looks like human written CSS
    | Expanded
    /// One rule per line
    | Compact
    /// Smallest amount of space possible, minimum whitespace
    | Compressed
    /// Undocumented
    | Inspect
    /// Undocumented
    | Sass

    member internal this.AsSharpScss =
        match this with
        | Nested -> SharpScss.ScssOutputStyle.Nested
        | Expanded -> SharpScss.ScssOutputStyle.Expanded
        | Compact -> SharpScss.ScssOutputStyle.Compact
        | Compressed -> SharpScss.ScssOutputStyle.Compressed
        | Inspect -> SharpScss.ScssOutputStyle.Inspect
        | Sass -> SharpScss.ScssOutputStyle.Sass

type ScssOptions =
    { /// The formatting style of the output CSS
      OutputStyle : ScssOutputStyle
      /// Add comments in output CSS indicating corresponding source line
      SourceComments : bool }

    member internal this.AsSharpScss =
        SharpScss.ScssOptions(
            OutputStyle = this.OutputStyle.AsSharpScss,
            SourceComments = this.SourceComments)

module ScssOptions =
    /// Default ScssOptions used in functions without options
    let defaults =
        { OutputStyle = Compressed
          SourceComments = false }

module StaticSite =
    /// Compile some Sass/SCSS styles and add the resulting CSS as a file
    let withSassOptions (options : ScssOptions) sass url site =
        let res = Scss.ConvertToCss(sass, options.AsSharpScss)
        site |> StaticSite.withFile res.Css url

    /// Compile some Sass/SCSS styles and add the resulting CSS as a file
    let withSass sass url site = 
        site |> withSassOptions ScssOptions.defaults sass url
        
    /// Compile a Sass/SCSS file and add the resulting CSS
    let withSassFileOptions (options : ScssOptions) sourceFile url site =
        let res = Scss.ConvertFileToCss(sourceFile, options.AsSharpScss)
        site |> StaticSite.withFile res.Css url

    /// Compile a Sass/SCSS file and add the resulting CSS
    let withSassFile sourceFile url site =
        site |> withSassFileOptions ScssOptions.defaults sourceFile url
