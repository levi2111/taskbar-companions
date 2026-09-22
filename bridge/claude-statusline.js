// Claude Code status line: shows plan usage and publishes it for the Claude companion.
// Claude Code passes `rate_limits` (Pro/Max, after the first response of a session) on stdin.
const fs = require('fs');
const path = require('path');

const target = path.join(__dirname, '..', 'app', 'data', 'claude.usage.json');

const quota = window => window && typeof window.used_percentage === 'number'
    ? {
        remainingPercent: Math.min(100, Math.max(0, 100 - window.used_percentage)),
        resetsAt: typeof window.resets_at === 'number' ? new Date(window.resets_at * 1000).toISOString() : null
    }
    : null;

function publish(limits) {
    const snapshot = { weekly: quota(limits.seven_day), session: quota(limits.five_hour), updatedAt: new Date().toISOString() };
    // Atomic replace so the widget never reads a partial file.
    const temp = `${target}.${process.pid}.tmp`;
    try {
        fs.mkdirSync(path.dirname(target), { recursive: true });
        fs.writeFileSync(temp, JSON.stringify(snapshot, null, 2));
        fs.renameSync(temp, target);
    } catch {
        try { fs.unlinkSync(temp); } catch { }
    }
}

let input = '';
process.stdin.on('data', chunk => input += chunk).on('end', () => {
    let data = {};
    try { data = JSON.parse(input); } catch { }
    const limits = data.rate_limits;
    // No limits yet in this session: keep whatever an earlier session published.
    if (limits && (limits.five_hour || limits.seven_day)) publish(limits);

    const parts = [data.model?.display_name ?? 'Claude'];
    const used = (label, window) => window && typeof window.used_percentage === 'number' && parts.push(`${label} ${Math.round(window.used_percentage)}%`);
    used('5h', limits?.five_hour);
    used('7d', limits?.seven_day);
    process.stdout.write(parts.join(' · '));
});
