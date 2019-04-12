namespace Fake.StaticGen.Markdown

open Markdig
open Markdig.Parsers
open Markdig.Syntax
open Markdig.Syntax.Inlines
open System.Runtime.CompilerServices

/// An extension for Markdig will rewrite URLs using the urlRewriter function, which takes
/// the LinkInline object and returns a new URL for the link or image to follow.
/// To get the old URL of the link, use the link.Url property. To check if the link is an
/// image, use the link.IsImage property.
type LinkUrlRewriteExtension(urlRewriter) =

    let documentProcessed (document : MarkdownDocument) =
        for node in (document :> MarkdownObject).Descendants() do
            match node with
            | :? LinkInline as link -> link.Url <- urlRewriter link
            | _ -> ()

    let deleg = ProcessDocumentDelegate(documentProcessed)
    interface IMarkdownExtension with

        member __.Setup(pipeline : MarkdownPipelineBuilder) =
            pipeline.remove_DocumentProcessed deleg
            pipeline.add_DocumentProcessed deleg

        member __.Setup(_, _) = ()

[<Extension>]
type MarkdownPipelineBuilderExtensions() =
    [<Extension>]
    /// Use the LinkUrlRewriteExtension to rewrite URLs for links and images
    static member UseLinkUrlRewrite(pipeline : MarkdownPipelineBuilder, urlRewriter) =
        pipeline.Extensions.Add(LinkUrlRewriteExtension(urlRewriter))
        pipeline
