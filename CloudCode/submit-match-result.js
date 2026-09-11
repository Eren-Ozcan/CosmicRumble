// CosmicRumble — dual-attestation trophy submission.
//
// Why this exists: trophies used to be written straight from the client that claimed it
// won, so a modified client could report an arbitrary score. Here BOTH players report
// their OWN outcome for the same match, and trophies only move when the two reports
// agree that one beat the other. A single lying client can no longer move its own
// trophies; it would have to collude with its opponent, who has no reason to agree.
//
// Deploy: see docs/cloud-code-setup.md. Not deployed yet -> the client silently falls
// back to its old direct submission path (LeaderboardManager.SubmitScoreAsync).
//
// Parameters:
//   matchId     string  the match's unique id, identical on both machines
//   opponentId  string  the reporter's opponent's UGS player id
//   won         boolean did the REPORTER win (the reporter never reports for the other side)
//
// Returns: { status, applied, reason? }
//   status  "pending"   first report stored, waiting for the other side
//           "settled"   both reports agreed, trophies applied
//           "rejected"  the two reports contradict each other, nothing applied

const { DataApi }        = require("@unity-services/cloud-save-1.4");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

const LEADERBOARD_ID = "cosmic_trophies";
const TROPHIES_WIN   = 30;
const TROPHIES_LOSS  = 20;

// A match record lives long enough for the slower client to report and no longer.
const RECORD_TTL_SECONDS = 60 * 60;

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId, accessToken } = context;
  const { matchId, opponentId, won } = params;

  if (!matchId || !opponentId || typeof won !== "boolean") {
    return { status: "rejected", applied: false, reason: "missing-parameters" };
  }
  if (opponentId === playerId) {
    return { status: "rejected", applied: false, reason: "self-report" };
  }

  const cloudSave = new DataApi({ accessToken });
  const key       = `match_${matchId}`;

  // The record is kept in CUSTOM data (not a player's own data): both players must be
  // able to read and write the same row, which per-player data cannot do.
  const existing = await readRecord(cloudSave, projectId, key, logger);
  const report   = { playerId, opponentId, won, at: Date.now() };

  if (!existing) {
    await writeRecord(cloudSave, projectId, key, { reports: [report] }, logger);
    return { status: "pending", applied: false };
  }

  if (existing.applied) {
    // Already settled -- a retry (flaky network, app resumed) must not pay out twice.
    return { status: "settled", applied: false, reason: "already-applied" };
  }

  const reports = existing.reports || [];
  if (reports.some(r => r.playerId === playerId)) {
    return { status: "pending", applied: false, reason: "duplicate-report" };
  }

  const other = reports.find(r => r.playerId === opponentId);
  if (!other) {
    reports.push(report);
    await writeRecord(cloudSave, projectId, key, { reports }, logger);
    return { status: "pending", applied: false };
  }

  // The two sides must name each other and disagree about who won -- exactly one winner.
  const namesEachOther = other.opponentId === playerId;
  const exactlyOneWon  = other.won !== won;

  if (!namesEachOther || !exactlyOneWon) {
    reports.push(report);
    await writeRecord(cloudSave, projectId, key,
                      { reports, applied: false, rejected: true }, logger);
    logger.warning(`Contradicting reports for ${matchId}: ${JSON.stringify(reports)}`);
    return { status: "rejected", applied: false, reason: "reports-disagree" };
  }

  const winnerId = won ? playerId : opponentId;
  const loserId  = won ? opponentId : playerId;

  const leaderboards = new LeaderboardsApi({ accessToken });
  await applyDelta(leaderboards, projectId, winnerId, +TROPHIES_WIN, logger);
  await applyDelta(leaderboards, projectId, loserId,  -TROPHIES_LOSS, logger);

  reports.push(report);
  await writeRecord(cloudSave, projectId, key,
                    { reports, applied: true, winnerId }, logger);

  return { status: "settled", applied: true };
};

// ---------------------------------------------------------------------------

async function readRecord(cloudSave, projectId, key, logger) {
  try {
    const res = await cloudSave.getCustomItems(projectId, "matches", [key]);
    const item = res.data && res.data.results && res.data.results[0];
    return item ? item.value : null;
  } catch (e) {
    logger.error(`Reading ${key} failed: ${e}`);
    return null;
  }
}

async function writeRecord(cloudSave, projectId, key, value, logger) {
  try {
    await cloudSave.setCustomItem(projectId, "matches", {
      key,
      value,
      ttl: RECORD_TTL_SECONDS,
    });
  } catch (e) {
    logger.error(`Writing ${key} failed: ${e}`);
  }
}

// Leaderboards store an absolute score, so a delta is read-modify-write. A missing
// entry means the player has never scored: they start from zero.
async function applyDelta(leaderboards, projectId, playerId, delta, logger) {
  let current = 0;
  try {
    const res = await leaderboards.getLeaderboardPlayerScore(
      projectId, LEADERBOARD_ID, playerId);
    current = (res.data && res.data.score) || 0;
  } catch (e) {
    logger.info(`No existing score for ${playerId} (${e}) -- starting at 0`);
  }

  const next = Math.max(0, current + delta);
  await leaderboards.addLeaderboardPlayerScore(
    projectId, LEADERBOARD_ID, playerId, { score: next });
}
