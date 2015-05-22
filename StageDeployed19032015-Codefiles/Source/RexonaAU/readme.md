# Rexona 'I Will Do' Frontend

Frontend build for the Rexona I Will Do website.

## General Stack

You'll need:

- A SASS compiler (with Compass)
- Local server and virtual host set up (e.g. XAMPP)

Note: FontAwesome is being used for icons. Use it for all icons, even if the PSD calls for a different icon.

## Git Workflow

Work entirely off of the `develop` branch. Every time you need to work on a new feature, idea, or bug, make *your own branch* from `develop`. 

Branches should be prefixed with your initials, followed by a descriptive name. E.g. If you're John Appleseed and you're fixing form validation, your branch would be called `ja-validation-fix`.

**Make sure you pull `develop` and merge it into your own branch before merging back into `develop`.** This is so that conflicts are handled in each developer's own working copy, and not on the main branch.

## Style

### General

Use tabs for indendation, not spaces.

Comment as much and as often as you can. Where possible, explain the *why* behind a snippet, not just the *what*. 

Everything you write should be self-contained and modular. Please try to avoid code that is not reusable across different contexts.

### CSS/SASS

Keep your CSS as modular and as reusable as possible. There are a main set of partials set up that should cover everything you'll need to work on. Please keep things in their appropriate partial (e.g. all type-related styling should go into `partials/_typography.sass`)

There is `partials/_hacks.sass` for your 'one off' CSS hacks but, obviously, keep this to a minimum. Ideally this file will be empty before we move into production.

### HTML

We're using PHP only for including partials (head tags, navigation, footer, scripts etc). You'll need to be running a local server with PHP support e.g. XAMPP.

### Images

Use .svg wherever possible, for example for icons, illustrations, etc. 




