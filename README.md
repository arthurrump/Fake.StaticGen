# Fake.StaticGen

A fully customizable static site generator using the power of [FAKE](https://fake.build) scripts to easily generate your site. Fake.StaticGen is built with three levels of content in mind: plain files, HTML pages and higher level 'posts', that will be transformed into pages, but can also be grouped together into overview pages.

## Building the sample

The samples depend on a local NuGet package of Fake.StaticGen being available, so you'll first have to run `dotnet pack` in the `src/Fake.StaticGen` folder to generate the package. Then the sample should just build with `fake build` (assuming a global FAKE install), putting the generated files in the `public` folder.

## Some ideas
- Should there be url magic, or let people handle that themselves?
- How to pass through the original filename, or maybe only use that as input for the url function?
- What to do with the view engine? A dependency on Giraffe is a bit weird
    - Paket file reference, or just incorporate the file with custom namespacing etc
    - Be more flexible and allow any type of view engine, supply the current one in a separate NuGet?
- Planned add-on packages:
    - Sass compiler
    - Less compiler
    - RSS generator
    - Markdown parser (with different types of frontmatter)
    - Dev server for easy local viewing
