const path = require("node:path");

// "staging" is deliberately NOT listed as a semantic-release prerelease branch here: RC
// version determination for staging lives in staging-ci.yml's own "version" job, which
// invokes semantic-release with a --branches override against this same config instead of a
// second branch entry in this file (see ci-target-schema.md section 4.8, and
// Softwareschmiede's release.config.js, the concrete reference implementation this file
// follows). That avoids @semantic-release/github creating its own GitHub release/tag from a
// staging push - staging-ci.yml's own gh CLI call fully owns the RC pre-release lifecycle
// instead, and only actual pushes/tags on main go through semantic-release's full publish
// pipeline below.

// Rezepte publishes multiple assets per release (linux/win zips, the update manifest, and the
// import-plugin contract export), so - like FinanceManager - RELEASE_ASSET_PATHS carries a
// list (newline/semicolon separated) rather than Softwareschmiede's single RELEASE_ASSET_PATH.
const releaseAssetPath = process.env.RELEASE_ASSET_PATH;
const releaseAssetPaths = (process.env.RELEASE_ASSET_PATHS ?? "")
  .split(/[;\n]/)
  .map((value) => value.trim())
  .filter(Boolean);
const releaseManifestPath = process.env.RELEASE_MANIFEST_PATH;
const releaseAssets = [...releaseAssetPaths, releaseAssetPath, releaseManifestPath]
  .filter(Boolean)
  .map((assetPath) => ({ path: assetPath, name: path.basename(assetPath) }));

const releasePlugins = [
  [
    "@semantic-release/commit-analyzer",
    {
      preset: "conventionalcommits"
    }
  ],
  [
    "@semantic-release/release-notes-generator",
    {
      preset: "conventionalcommits"
    }
  ],
  [
    "@semantic-release/github",
    {
      assets: releaseAssets,
      // The default GITHUB_TOKEN only has `contents: write` - it cannot comment on the PR(s)
      // associated with a released commit, which is what the plugin's "success"/"fail" steps
      // otherwise attempt by default. Without this, any release whose commit has an associated
      // PR (e.g. the routine staging->main promotion PR) fails at that final comment step with
      // a GraphQL "Resource not accessible by integration" error, even though the release
      // itself was already published successfully. Discovered via a real production incident
      // on msTools.Updater.
      successComment: false,
      failComment: false
    }
  ]
];

// Selected instead of releasePlugins whenever resolve-release-version.mjs runs its
// dry-run-only version check (RESOLVE_DRY_RUN=true, set in runSemanticReleaseDryRun()) or
// staging-ci.yml's "version" job runs its --branches override dry-run - avoids loading
// @semantic-release/github (and its verifyConditions checks) for a call that never publishes
// anything.
const dryRunPlugins = [
  [
    "@semantic-release/commit-analyzer",
    {
      preset: "conventionalcommits"
    }
  ]
];

module.exports = {
  branches: ["main"],
  tagFormat: "v${version}",
  plugins: process.env.RESOLVE_DRY_RUN === "true" ? dryRunPlugins : releasePlugins
};
