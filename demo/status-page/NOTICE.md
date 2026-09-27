# Notice

This status page is adapted from the public status page of **Piro**
(https://github.com/heva-co/piro, `apps/web`), Copyright © 2025 heva Inc.,
licensed under the GNU Affero General Public License v3.0.

The files in this folder (`index.html`, `app.js`) are a port of Piro's
`StatusHeader`, `ServiceRow`, `StatusBarCalendar`, `StatusDot`, `Nav`, `Footer`,
`computeOverallStatus` and `globals.css` from React/Next.js to a static page that
reads Vigia's public endpoint `GET /status/{slug}`.

They are distributed under the AGPL-3.0 (see `LICENSE`), separately from the rest
of this repository. The page only talks to Vigia over HTTP; no Vigia code includes it.
