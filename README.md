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
    - Markdown parser (with different types of frontmatter like YAML, TOML, JSON)
    - HTML DSL for defining templates in F#, other temlating systems (eg. dotliquid, razor)
    - Dev server for easy local viewing
