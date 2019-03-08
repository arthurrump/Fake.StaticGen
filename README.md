# Fake.StaticGen
A fully customizable static site generator using the power of [FAKE](https://fake.build) scripts to easily generate your site.

## Building the samples
The samples depend on a local NuGet package of Fake.StaticGen and required add-ons being available, so you'll first have to run `fake build` in the root of the repo to create all packages in the `/packages` folder. Then the samples should just build with `fake build` (assuming a global FAKE install), putting the generated files in the `public` folder.

## Some ideas
- Find a way to be more lazy when reading in files, and what needs to be kept for big overviews. There is a trade-off between flexibility to let the user do any file transformation they would want and memory footprint. Maybe there is a happy middle?
- Planned add-on packages:
    - Sass/Less compiler
      - SharpSCSS (and other libsass wrappers) don’t seem to work in a FAKE script environment. Other options: invoke the program (so it’ll need to be globally installed, that’s what Fornax does); use a JavaScript runtime (like [Jint](https://github.com/sebastienros/jint) or [Nil.JS](https://github.com/nilproject/NiL.JS)) to run the JS version (could then also easily adapt to Less, Stylus and other JS tooling like minifiers)
    - Easy to use Markdown with frontmatter ([YAML](https://noyaml.com), TOML, JSON) for pages
    - Watch mode local server
