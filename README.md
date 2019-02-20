# Fake.StaticGen
A fully customizable static site generator using the power of [FAKE](https://fake.build) scripts to easily generate your site.

## Building the samples
The samples depend on a local NuGet package of Fake.StaticGen and required add-ons being available, so you'll first have to build the solution by running `dotnet build` in the `/src` folder to generate the packages. Then the samples should just build with `fake build` (assuming a global FAKE install), putting the generated files in the `public` folder.

## Some ideas

- Currently all added files are immediately read into a string and stored in memory. Maybe it’s better to wait with this until the generation step and just store the source file coupled with the correct parser. This might reduce memory usage for large sites, but it would also make a dynamic server watching for changes easier and faster.
- Planned add-on packages:
    - Sass/Less compiler
      - SharpSCSS (and other libsass wrappers) don’t seem to work in a FAKE script environment. Other options: invoke the program (so it’ll need to be globally installed, that’s what Fornax does); use a JavaScript runtime (like [Jint](https://github.com/sebastienros/jint) or [Nil.JS](https://github.com/nilproject/NiL.JS)) to run the JS version (could then also easily adapt to Less, Stylus and other JS tooling like minifiers)
    - Easy to use Markdown with frontmatter ([YAML](https://noyaml.com), TOML, JSON) for pages
    - Watch mode local server
