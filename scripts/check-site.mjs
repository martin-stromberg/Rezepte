#!/usr/bin/env node
// Static validation for the assembled GitHub Pages output of the Rezepte site.
// Dependency-free, requires Node.js >= 20.
//
// Usage: node scripts/check-site.mjs [outputDirectory]   (default: artifacts/pages)
//
// Checks:
//   - index.html exists, declares lang="de", a non-empty <title> and a viewport meta tag
//   - every local src/href/srcset/data-animated-src reference resolves to a
//     file in the output (HTML comments and inline <script> bodies ignored)
//   - no local reference uses a leading "/" (the page must work under the /Rezepte/ base path)
//   - the screenshots directory exists and is not empty
//   - WCAG 2.x contrast of the light and dark palettes declared in the built CSS
//   - dark tokens in the prefers-color-scheme media query and the [data-theme="dark"]
//     block are value-identical (the palettes must not drift apart)

import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve, sep } from 'node:path';

const outputRoot = resolve(process.argv[2] ?? 'artifacts/pages');
const errors = [];
const notes = [];

function fail(message) {
    errors.push(message);
}

function pass(message) {
    notes.push(message);
}

function listFilesRecursive(dir) {
    const files = [];
    for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const stat = statSync(full);
        if (stat.isDirectory()) {
            files.push(...listFilesRecursive(full));
        } else if (stat.isFile()) {
            files.push(full);
        }
    }
    return files;
}

// ---------------------------------------------------------------------------
// Structure checks
// ---------------------------------------------------------------------------

if (!existsSync(outputRoot) || !statSync(outputRoot).isDirectory()) {
    fail(`Output directory '${outputRoot}' does not exist. Run scripts/Build-Site.ps1 first.`);
    reportAndExit();
}

const indexPath = join(outputRoot, 'index.html');
if (!existsSync(indexPath)) {
    fail('index.html is missing in the output directory.');
    reportAndExit();
}

const indexHtml = readFileSync(indexPath, 'utf8');

if (!/<html[^>]*\blang="de"/i.test(indexHtml)) {
    fail('index.html must declare <html lang="de">.');
} else {
    pass('index.html declares lang="de".');
}

const titleMatch = indexHtml.match(/<title>([^<]*)<\/title>/i);
if (!titleMatch || titleMatch[1].trim().length === 0) {
    fail('index.html has no non-empty <title>.');
} else {
    pass(`<title> is set ("${titleMatch[1].trim()}").`);
}

if (!/<meta[^>]+name="viewport"[^>]+content="[^"]*width=device-width[^"]*"/i.test(indexHtml)) {
    fail('index.html is missing a responsive <meta name="viewport" content="...width=device-width..."> tag.');
} else {
    pass('Viewport meta tag is present.');
}

// ---------------------------------------------------------------------------
// Reference resolution for every local src/href/srcset/data-animated-src in
// every built HTML file
// ---------------------------------------------------------------------------

const htmlFiles = listFilesRecursive(outputRoot).filter((file) => file.toLowerCase().endsWith('.html'));
// The (?<![\w-]) lookbehind keeps prefixed attributes (data-src, data-href,
// any future lazy-load placeholders) out of the generic src/href match —
// arbitrary data-* attributes are never treated as file references.
const attrPattern = /(?<![\w-])(?:src|href)\s*=\s*(?:"([^"]*)"|'([^']*)')/gi;
const srcsetPattern = /(?<![\w-])srcset\s*=\s*(?:"([^"]*)"|'([^']*)')/gi;
// data-animated-src is the one data-* attribute that does reference a file:
// it points at the animated variant the demo toggle swaps in on demand
// (see site/assets/js/demo.js) and is therefore checked explicitly.
const animatedSrcPattern = /(?<![\w-])data-animated-src\s*=\s*(?:"([^"]*)"|'([^']*)')/gi;
const externalPattern = /^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#)/;
let localReferenceCount = 0;

// HTML comments and inline <script> bodies are not markup references: strip
// them so commented-out attributes and script source text are never scanned.
// The <script> tags themselves (including external src= attributes) survive.
function scannableMarkup(html) {
    return html
        .replace(/<!--[\s\S]*?-->/g, '')
        .replace(/<script\b([^>]*)>[\s\S]*?<\/script\s*>/gi, '<script$1></script>');
}

// srcset is a comma-separated list of "url [descriptor]" candidates
// (e.g. "img.png, img@2x.png 2x" or "small.png 400w, large.png 800w").
function srcsetCandidates(value) {
    return value
        .split(',')
        .map((candidate) => candidate.trim().split(/\s+/)[0])
        .filter((url) => url.length > 0);
}

function checkLocalReference(htmlFile, htmlDir, raw) {
    if (raw.length === 0 || externalPattern.test(raw)) {
        return;
    }
    if (raw.startsWith('/')) {
        fail(`${rel(htmlFile)}: local reference "${raw}" uses a leading "/"; only relative paths are allowed (base path /Rezepte/).`);
        return;
    }
    const target = raw.split(/[?#]/, 1)[0];
    if (target.length === 0) {
        return;
    }
    localReferenceCount++;
    let decoded;
    try {
        decoded = decodeURIComponent(target);
    } catch {
        fail(`${rel(htmlFile)}: reference "${raw}" contains invalid percent-encoding.`);
        return;
    }
    const resolved = resolve(htmlDir, decoded);
    if (!resolved.startsWith(outputRoot + sep) && resolved !== outputRoot) {
        fail(`${rel(htmlFile)}: reference "${raw}" escapes the output directory.`);
        return;
    }
    if (!existsSync(resolved)) {
        fail(`${rel(htmlFile)}: referenced file "${raw}" does not exist in the output (expected at ${rel(resolved)}).`);
        return;
    }
    // Reference targets must be files; a link to a directory would 404 or
    // serve a listing on GitHub Pages.
    if (!statSync(resolved).isFile()) {
        fail(`${rel(htmlFile)}: reference "${raw}" resolves to a directory, not a file (${rel(resolved)}).`);
    }
}

for (const htmlFile of htmlFiles) {
    const html = scannableMarkup(readFileSync(htmlFile, 'utf8'));
    const htmlDir = dirname(htmlFile);
    const rawReferences = [];
    for (const match of html.matchAll(attrPattern)) {
        rawReferences.push(match[1] ?? match[2]);
    }
    for (const match of html.matchAll(srcsetPattern)) {
        rawReferences.push(...srcsetCandidates(match[1] ?? match[2]));
    }
    for (const match of html.matchAll(animatedSrcPattern)) {
        rawReferences.push(match[1] ?? match[2]);
    }
    for (const raw of rawReferences) {
        checkLocalReference(htmlFile, htmlDir, raw.trim());
    }
}
pass(`${localReferenceCount} local reference(s) resolved (src/href/srcset/data-animated-src).`);

const screenshotsDir = join(outputRoot, 'screenshots');
if (!existsSync(screenshotsDir) || !statSync(screenshotsDir).isDirectory()) {
    fail('screenshots/ directory is missing in the output.');
} else {
    const screenshotFiles = readdirSync(screenshotsDir).filter((name) => statSync(join(screenshotsDir, name)).isFile());
    if (screenshotFiles.length === 0) {
        fail('screenshots/ directory is empty.');
    } else {
        pass(`screenshots/ contains ${screenshotFiles.length} file(s).`);
    }
}

// ---------------------------------------------------------------------------
// Palette extraction and WCAG contrast checks
// ---------------------------------------------------------------------------

const stylesheetMatch = indexHtml.match(/<link[^>]+rel="stylesheet"[^>]+href="([^"]+)"/i)
    ?? indexHtml.match(/<link[^>]+href="([^"]+)"[^>]+rel="stylesheet"/i);
const stylesheetPath = stylesheetMatch
    ? resolve(outputRoot, stylesheetMatch[1])
    : join(outputRoot, 'assets', 'css', 'site.css');

if (!existsSync(stylesheetPath)) {
    fail(`Stylesheet '${rel(stylesheetPath)}' referenced by index.html was not found; palette checks cannot run.`);
    reportAndExit();
}

const css = readFileSync(stylesheetPath, 'utf8').replace(/\/\*[\s\S]*?\*\//g, '');

// Returns the inner content of the first block whose "{" opens at `braceIndex`.
function blockContent(source, braceIndex) {
    let depth = 0;
    for (let i = braceIndex; i < source.length; i++) {
        if (source[i] === '{') depth++;
        else if (source[i] === '}') {
            depth--;
            if (depth === 0) {
                return source.slice(braceIndex + 1, i);
            }
        }
    }
    return null;
}

function extractDeclarations(block) {
    const declarations = new Map();
    const pattern = /(--[A-Za-z0-9_-]+)\s*:\s*([^;]+);/g;
    for (const match of block.matchAll(pattern)) {
        declarations.set(match[1], match[2].trim());
    }
    return declarations;
}

function extractBlockDeclarations(source, selectorRegex, description) {
    const match = selectorRegex.exec(source);
    if (!match) {
        fail(`Could not find ${description} in ${rel(stylesheetPath)}.`);
        return null;
    }
    const braceIndex = source.indexOf('{', match.index + match[0].length - 1);
    if (braceIndex < 0 || braceIndex > match.index + match[0].length + 200) {
        fail(`Could not parse the ${description} block in ${rel(stylesheetPath)}.`);
        return null;
    }
    const block = blockContent(source, braceIndex);
    if (block === null) {
        fail(`Unbalanced braces in the ${description} block of ${rel(stylesheetPath)}.`);
        return null;
    }
    return extractDeclarations(block);
}

const lightTokens = extractBlockDeclarations(css, /:root\s*\{/, 'light :root palette');
const darkAttrTokens = extractBlockDeclarations(css, /\[data-theme="dark"\]\s*\{/, '[data-theme="dark"] palette');

// Dark media-query block: locate "@media (prefers-color-scheme: dark) { ... }" and
// extract the custom properties declared on :root inside it.
let darkMediaTokens = null;
const mediaRegex = /@media\s*\(prefers-color-scheme:\s*dark\)\s*\{/g;
for (const match of css.matchAll(mediaRegex)) {
    const braceIndex = css.indexOf('{', match.index);
    const mediaBlock = blockContent(css, braceIndex);
    if (mediaBlock === null) {
        fail('Unbalanced braces in a @media (prefers-color-scheme: dark) block.');
        continue;
    }
    const innerMatch = /:root[^{]*\{/.exec(mediaBlock);
    if (!innerMatch) {
        continue; // e.g. the media block that only restyles the toggle icon
    }
    const innerBrace = mediaBlock.indexOf('{', innerMatch.index);
    const innerBlock = blockContent(mediaBlock, innerBrace);
    if (innerBlock === null) {
        fail('Unbalanced braces in the dark-palette :root block inside the media query.');
        continue;
    }
    const declarations = extractDeclarations(innerBlock);
    if (declarations.size > 0) {
        darkMediaTokens = declarations;
        break;
    }
}

if (!darkMediaTokens) {
    fail('No dark palette found inside a @media (prefers-color-scheme: dark) block.');
}

// The two dark definitions must be identical so the palettes cannot drift apart.
if (darkMediaTokens && darkAttrTokens) {
    const names = new Set([...darkMediaTokens.keys(), ...darkAttrTokens.keys()]);
    let identical = true;
    for (const name of names) {
        const mediaValue = darkMediaTokens.get(name);
        const attrValue = darkAttrTokens.get(name);
        if (mediaValue !== attrValue) {
            fail(`Dark token ${name} diverges: media query has "${mediaValue}", [data-theme="dark"] has "${attrValue}".`);
            identical = false;
        }
    }
    if (identical) {
        pass(`Dark palette is identical in the media query and the [data-theme="dark"] block (${names.size} tokens).`);
    }
}

// ---------------------------------------------------------------------------
// WCAG 2.x contrast (relative luminance)
// ---------------------------------------------------------------------------

function hexToRgb(hex) {
    const normalized = hex.trim().replace(/^#/, '');
    const expanded = normalized.length === 3
        ? normalized.split('').map((c) => c + c).join('')
        : normalized;
    if (!/^[0-9a-fA-F]{6}$/.test(expanded)) {
        return null;
    }
    return [
        parseInt(expanded.slice(0, 2), 16),
        parseInt(expanded.slice(2, 4), 16),
        parseInt(expanded.slice(4, 6), 16),
    ];
}

function relativeLuminance(hex) {
    const rgb = hexToRgb(hex);
    if (!rgb) {
        return null;
    }
    const [r, g, b] = rgb.map((channel) => {
        const c = channel / 255;
        return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrastRatio(hexA, hexB) {
    const lumA = relativeLuminance(hexA);
    const lumB = relativeLuminance(hexB);
    if (lumA === null || lumB === null) {
        return null;
    }
    const [lighter, darker] = lumA >= lumB ? [lumA, lumB] : [lumB, lumA];
    return (lighter + 0.05) / (darker + 0.05);
}

// Minimum pairs per palette (see plan): normal text needs >= 4.5:1,
// large text and UI components >= 3:1.
// The last two pairs are component bindings for the hero .btn-primary, which
// uses a fixed light face (--app-on-primary) with primary text/hover color in
// both themes. They were added after a review found the previous face/text
// binding (--app-surface + --app-primary) collapsing to ~1.2:1 in dark mode —
// a token combination the generic pairs above do not cover.
const CONTRAST_PAIRS = [
    { foreground: '--app-text', background: '--app-bg', min: 4.5, label: 'text on background' },
    { foreground: '--app-text', background: '--app-surface', min: 4.5, label: 'text on surface' },
    { foreground: '--app-muted', background: '--app-bg', min: 4.5, label: 'muted text on background' },
    { foreground: '--app-muted', background: '--app-surface', min: 4.5, label: 'muted text on surface' },
    { foreground: '--app-link', background: '--app-bg', min: 4.5, label: 'link on background' },
    { foreground: '--app-link', background: '--app-surface', min: 4.5, label: 'link on surface' },
    { foreground: '--app-on-primary', background: '--app-primary', min: 4.5, label: 'light text on gradient start (navbar/hero/buttons)' },
    { foreground: '--app-on-primary', background: '--app-primary-600', min: 4.5, label: 'light text on gradient end (navbar/hero/buttons)' },
    { foreground: '--app-border', background: '--app-surface', min: 3.0, label: 'border on surface (UI component)' },
    { foreground: '--app-primary', background: '--app-on-primary', min: 4.5, label: 'hero .btn-primary label on its light button face' },
    { foreground: '--app-primary-600', background: '--app-on-primary', min: 4.5, label: 'hero .btn-primary:hover label on its light button face' },
];

function checkPalette(paletteName, tokens) {
    if (!tokens) {
        return;
    }
    for (const pair of CONTRAST_PAIRS) {
        const fg = tokens.get(pair.foreground);
        const bg = tokens.get(pair.background);
        if (!fg || !bg) {
            fail(`${paletteName} palette: token ${!fg ? pair.foreground : pair.background} required for the "${pair.label}" check is not declared.`);
            continue;
        }
        const ratio = contrastRatio(fg, bg);
        if (ratio === null) {
            fail(`${paletteName} palette: cannot compute contrast for "${pair.label}" (${pair.foreground}=${fg}, ${pair.background}=${bg}) - only #rgb/#rrggbb colors are supported.`);
            continue;
        }
        const formatted = ratio.toFixed(2);
        if (ratio + 1e-9 < pair.min) {
            fail(`${paletteName} palette: ${pair.label} fails WCAG (${pair.foreground} ${fg} on ${pair.background} ${bg} = ${formatted}:1, required >= ${pair.min}:1). Adjust the token, not the threshold.`);
        } else {
            pass(`${paletteName}: ${pair.label} = ${formatted}:1 (>= ${pair.min}:1).`);
        }
    }
}

checkPalette('light', lightTokens);
checkPalette('dark', darkAttrTokens ?? darkMediaTokens);

// ---------------------------------------------------------------------------

function rel(path) {
    const relative = path.startsWith(outputRoot)
        ? path.slice(outputRoot.length).replace(/^[\\/]+/, '')
        : path;
    return relative || '.';
}

function reportAndExit() {
    for (const note of notes) {
        console.log(`PASS ${note}`);
    }
    if (errors.length > 0) {
        for (const error of errors) {
            console.error(`FAIL ${error}`);
        }
        console.error(`\n${errors.length} check(s) failed for ${outputRoot}.`);
        process.exit(1);
    }
    console.log(`\nAll checks passed for ${outputRoot}.`);
    process.exit(0);
}

reportAndExit();
