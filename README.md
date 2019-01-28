# Fake.StaticGen
A fully customizable static site generator using the power of [FAKE](https://fake.build) scripts to easily generate your site.

## Building the samples
The samples depend on a local NuGet package of Fake.StaticGen being available, so you'll first have to run `dotnet pack` in the `src/Fake.StaticGen` folder to generate the package. Then the samples should just build with `fake build` (assuming a global FAKE install), putting the generated files in the `public` folder.

## Some ideas
- Should there be url magic, or let people handle that themselves?
    - Probably have some URL helpers
- Planned add-on packages:
    - Sass compiler
    - Less compiler
    - RSS generator
    - Markdown parser (with different types of frontmatter)
    - Dev server for easy local viewing
    
And the weirdness that's currently in the simple sample:
```fsharp
// Weirdness:
// - Due to the parser always returning a 'page, the input to the url function is a 'page,
//   even though it will always be of type Post, so the url function has redundant cases or
//   not fully matching.
//   -> Move the creation of urls to a function passed to the generate function
//      + All urls in one place
//        ~ They can also be close together in the main pipeline, so not that much of a benefit
//      - Urls are further away from the thing they refer to
//      - Would be hard to do for files, so that makes differences between Pages and Files bigger
//      - How to get the original file names into the url?
//   -> Make the parser return the url too
//      - Feels out of place, that's not what a parser is for
//   -> Have the parser return useful things and add a wrap function that would put it into 'page
//      - More functions, might not always apply
```