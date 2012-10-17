WebMatrix Extension: [http://extensions.webmatrix.com/packages/TypeScript4WebMatrix](http://extensions.webmatrix.com/packages/TypeScript4WebMatrix)

DocumentUp version of the documentation: [http://macawnl.github.com/TypeScript4WebMatrix](http://macawnl.github.com/TypeScript4WebMatrix)

# TypeScript Tools for WebMatrix

## Introduction

TypeScript is a new language for application-scale Javascript development. TypeScript is designed by Anders Hejlsberg, inventor of C#.

Although there is great support for TypeScript in Visual Studio through an installable [MSI](http://go.microsoft.com/fwlink/?LinkID=266563), we also need support for TypeScript in WebMatrix.

## Why WebMatrix

TypeScript support in WebMatrix is important because WebMatrix can be used really well to write Node.js apps, something that is not supported in Visual Studio 2012.
## Features

The **TypeScript** [WebMatrix extension](http://extensions.webmatrix.com/packages/TypeScript4WebMatrix) provides compilation support for TypeScript within WebMatrix. This package provides the following functionality:

* Compile all TypeScript files in the project
* Compile all TypeScript files in a folder
* Compile a TypeScript file
* Update the TypeScript software

By default TypeScript files are not opened in WebMatrix itself. If you right-click on a TypeScript file you will see the action to **Open with Webmatrix**. From that point on TypeScript files will be opened with WebMatrix until you restart WebMatrix.

You can also configure WebMatrix as the default application for opening .ts files. Do this by right-click on a TypeScript file in the File Explorer and configure WebMatrix as the default application to open .ts files with.

## Editing
WebMatrix does not support syntax highlighting and intellisense on TypeScript files. It is possible to use another editor to edit your TypeScript file that provides this functionality:

* [Visual Studio 2012](http://www.microsoft.com/visualstudio) - syntax highlighting and intellisense (TypeScript support seems to work on [Visual Studio 1212 Express edition](http://www.microsoft.com/visualstudio/eng/products/visual-studio-express-products)

* [Sublime Text 2](http://www.sublimetext.com/2) - only syntax highlighting (see the blogpost [http://blogs.msdn.com/b/interoperability/archive/2012/10/01/sublime-text-vi-emacs-typescript-enabled.aspx](http://blogs.msdn.com/b/interoperability/archive/2012/10/01/sublime-text-vi-emacs-typescript-enabled.aspx)) ==> also support for Emacs and Vim



