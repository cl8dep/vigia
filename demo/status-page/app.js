// Status page adapted from Piro (https://github.com/heva-co/piro), Copyright (c) 2025 heva Inc., AGPL-3.0.
// Ports StatusHeader, ServiceRow, StatusBarCalendar, StatusDot and computeOverallStatus from React to plain JS,
// reading Vigia's public endpoint GET /status/{slug}. See NOTICE.md.

const PAGE = new URLSearchParams(location.search).get("page") || "public";
const REFRESH_MS = 30_000;

// Vigia states mapped onto Piro's status vocabulary.
const TO_PIRO = {
  operational: "UP",
  degraded: "DEGRADED",
  "partial-outage": "PARTIALLY_DOWN",
  down: "DOWN",
  maintenance: "MAINTENANCE",
  unknown: "NO_DATA",
};

// StatusHeader.tsx
const pingColor = {
  UP: "bg-green-500",
  DEGRADED: "bg-amber-500",
  PARTIALLY_DOWN: "bg-orange-500",
  DOWN: "bg-red-500",
  MAINTENANCE: "bg-indigo-500",
  NO_DATA: "bg-gray-400",
};

// ServiceRow.tsx
const statusColor = {
  UP: "text-green-500",
  DEGRADED: "text-amber-500",
  PARTIALLY_DOWN: "text-orange-500",
  DOWN: "text-red-500",
  MAINTENANCE: "text-indigo-500",
  NO_DATA: "text-muted-foreground",
};

// lucide-react icons used by ServiceRow, as inline SVG.
const ICONS = {
  UP: '<circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/>',
  DEGRADED: '<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"/><path d="M12 9v4"/><path d="M12 17h.01"/>',
  PARTIALLY_DOWN: '<circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/>',
  DOWN: '<circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/>',
  MAINTENANCE: '<path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>',
  NO_DATA: '<circle cx="12" cy="12" r="10"/><path d="M8 12h8"/>',
};
const SUN = '<circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/>';
const MOON = '<path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/>';

function svg(paths, cls) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="${cls}">${paths}</svg>`;
}

function esc(text) {
  const div = document.createElement("div");
  div.textContent = text ?? "";
  return div.innerHTML;
}

// lib/utils.ts computeOverallStatus, adapted: partial outages count as disruptions.
function computeOverallStatus(components) {
  const statuses = components.map((c) => TO_PIRO[c.state]);
  const down = statuses.filter((s) => s === "DOWN").length;
  const partial = statuses.filter((s) => s === "PARTIALLY_DOWN").length;
  const degraded = statuses.filter((s) => s === "DEGRADED").length;
  const total = statuses.length;
  const majorThreshold = total > 1 ? total / 2 : 1;

  if (down > 0) {
    return {
      status: "DOWN",
      text: down >= majorThreshold ? "Major system outage" : down > 1 ? "Multiple services disrupted" : "Service disruption",
    };
  }
  if (partial > 0) {
    return { status: "PARTIALLY_DOWN", text: partial > 1 ? "Multiple partial outages" : "Partial outage" };
  }
  if (degraded > 0) {
    return { status: "DEGRADED", text: degraded > 1 ? "Multiple services degraded" : "Partial service degradation" };
  }
  if (statuses.some((s) => s === "MAINTENANCE")) {
    return { status: "MAINTENANCE", text: "Under maintenance" };
  }
  return { status: total > 0 && statuses.every((s) => s === "UP") ? "UP" : "NO_DATA", text: total > 0 ? "All systems operational" : "No services configured yet" };
}

function renderHeader(overall) {
  return `
    <div class="rounded-3xl border px-5 py-5 flex items-center gap-4">
      <span class="relative flex size-4 shrink-0">
        <span class="absolute inline-flex h-full w-full animate-ping rounded-full opacity-75 ${pingColor[overall.status]}"></span>
        <span class="relative inline-flex size-4 rounded-full ${pingColor[overall.status]}"></span>
      </span>
      <span class="text-xl sm:text-2xl font-medium">${esc(overall.text)}</span>
    </div>`;
}

function shortDate(iso) {
  return new Date(`${iso}T00:00:00Z`).toLocaleDateString(undefined, { month: "short", day: "numeric", timeZone: "UTC" });
}

function renderRow(component, index) {
  const status = TO_PIRO[component.state] ?? "NO_DATA";
  const uptime = component.uptime == null ? "" : `${(component.uptime * 100).toFixed(1)}%`;
  const partitions = Object.entries(component.partitions ?? {});
  const history = component.history ?? [];
  return `
    <div class="rounded-2xl border overflow-hidden">
      <div class="flex flex-col gap-3 px-5 py-4 hover:bg-muted/20 transition-colors">
        <div class="flex items-center gap-3">
          <div class="flex-1 min-w-0">
            <p class="font-medium text-sm text-foreground truncate">${esc(component.name)}</p>
            ${partitions.length ? `<div class="flex flex-wrap gap-3 mt-1">${partitions.map(([name, state]) => `
              <span class="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                <span title="${esc(state)}" class="rounded-full shrink-0 inline-block size-2 ${pingColor[TO_PIRO[state]] ?? "bg-gray-400"}"></span>${esc(name)}
              </span>`).join("")}</div>` : ""}
          </div>
          <div class="flex items-center gap-3 shrink-0">
            ${uptime ? `<span class="text-sm font-semibold text-foreground">${uptime}</span>` : ""}
            ${svg(ICONS[status], `size-5 ${statusColor[status]}`)}
          </div>
        </div>
        ${history.length ? `
        <div class="flex flex-col gap-1.5">
          <div class="relative w-full" data-bar="${index}"></div>
          <div class="flex justify-between text-xs text-muted-foreground">
            <span>${shortDate(history[0].date)}</span>
            <span>${shortDate(history[history.length - 1].date)}</span>
          </div>
        </div>` : ""}
      </div>
    </div>`;
}

// StatusBarCalendar.tsx: canvas bar with eased hover highlight and a tooltip per day.
const COLOR = {
  operational: "#22c55e",
  down: "#ef4444",
  "partial-outage": "#f97316",
  degraded: "#eab308",
  maintenance: "#3b82f6",
};
const STATE_LABEL = {
  operational: "Operational",
  degraded: "Degraded",
  "partial-outage": "Partial outage",
  down: "Outage",
  maintenance: "Maintenance",
};

// Observers of the bars currently on screen; disconnected before each refresh re-renders them.
let cleanups = [];

function noDataColor() {
  return document.documentElement.classList.contains("dark") ? "#3f3f46" : "#e4e4e7";
}

function mountBar(container, data, barHeight = 36, radius = 6) {
  const canvas = document.createElement("canvas");
  canvas.style.width = "100%";
  canvas.style.height = `${barHeight + 8}px`;
  canvas.setAttribute("aria-label", "Status history bar chart");
  const clip = document.createElement("div");
  clip.className = "overflow-hidden";
  clip.style.borderRadius = `${radius}px`;
  clip.appendChild(canvas);
  container.appendChild(clip);

  const tooltip = document.getElementById("tooltip");
  const dpr = window.devicePixelRatio || 1;
  const state = data.map(() => ({ scale: 1, opacity: 1 }));
  let width = container.clientWidth;
  let hovered = null;
  let frame = null;

  const target = (i) => {
    if (hovered === null) return { scale: 1, opacity: 1 };
    if (i === hovered) return { scale: 1.15, opacity: 1 };
    if (i === hovered - 1 || i === hovered + 1) return { scale: 1.08, opacity: 0.9 };
    return { scale: 1, opacity: 0.5 };
  };

  function draw() {
    if (!width) return;
    const ctx = canvas.getContext("2d");
    const padding = 4;
    const totalHeight = barHeight + padding * 2;
    canvas.width = Math.floor(width * dpr);
    canvas.height = Math.floor(totalHeight * dpr);
    ctx.scale(dpr, dpr);
    ctx.clearRect(0, 0, width, totalHeight);
    const barWidth = Math.max(1, width / data.length);

    data.forEach((day, i) => {
      const x = Math.round(i * barWidth);
      const bw = Math.max(0, Math.round((i + 1) * barWidth) - x);
      const { scale, opacity } = state[i];
      const h = barHeight * scale;
      const y = padding + (barHeight - h) / 2;
      const first = i === 0;
      const last = i === data.length - 1;
      ctx.globalAlpha = opacity;
      if (first || last) {
        ctx.save();
        ctx.beginPath();
        ctx.roundRect(x, y, bw, h, [first ? radius * scale : 0, last ? radius * scale : 0, last ? radius * scale : 0, first ? radius * scale : 0]);
        ctx.clip();
      }

      if (!day.state) {
        ctx.fillStyle = noDataColor();
        ctx.fillRect(x, y, bw, h);
      } else {
        // Unavailable share on the bottom in the day's color, available share on top in green.
        const unavailable = day.state === "down" || day.state === "partial-outage" ? 1 - (day.uptime ?? 1) : 0;
        const badH = unavailable > 0 ? Math.max(2, Math.round(unavailable * h)) : day.state === "operational" ? 0 : h;
        ctx.fillStyle = COLOR.operational;
        ctx.fillRect(x, y, bw, h - badH);
        ctx.fillStyle = COLOR[day.state] ?? noDataColor();
        ctx.fillRect(x, y + h - badH, bw, badH);
      }

      if (first || last) ctx.restore();
    });
    ctx.globalAlpha = 1;
  }

  function animate() {
    if (frame !== null) return;
    const step = () => {
      let settled = true;
      state.forEach((s, i) => {
        const t = target(i);
        s.scale += (t.scale - s.scale) * 0.2;
        s.opacity += (t.opacity - s.opacity) * 0.2;
        if (Math.abs(t.scale - s.scale) < 0.001) s.scale = t.scale;
        if (Math.abs(t.opacity - s.opacity) < 0.001) s.opacity = t.opacity;
        if (s.scale !== t.scale || s.opacity !== t.opacity) settled = false;
      });
      draw();
      frame = settled ? null : requestAnimationFrame(step);
    };
    frame = requestAnimationFrame(step);
  }

  canvas.addEventListener("mousemove", (e) => {
    const rect = canvas.getBoundingClientRect();
    const index = Math.min(data.length - 1, Math.floor(((e.clientX - rect.left) / rect.width) * data.length));
    if (index !== hovered) {
      hovered = index;
      animate();
    }
    const day = data[index];
    const label = day.state ? STATE_LABEL[day.state] : "No data";
    const color = day.state ? statusColor[TO_PIRO[day.state]] : "text-muted-foreground";
    const uptime = day.uptime == null ? "" : ` <span class="text-muted-foreground">·</span> ${(day.uptime * 100).toFixed(2)}%`;
    tooltip.innerHTML = `<span class="${color}">${label}</span> <span class="text-muted-foreground">·</span> ${shortDate(day.date)}${uptime}`;
    tooltip.classList.remove("hidden");
    const barCenter = rect.left + ((index + 0.5) / data.length) * rect.width;
    tooltip.style.left = `${Math.min(window.innerWidth - tooltip.offsetWidth - 8, Math.max(8, barCenter - tooltip.offsetWidth / 2))}px`;
    tooltip.style.top = `${rect.top - tooltip.offsetHeight - 6}px`;
  });
  canvas.addEventListener("mouseleave", () => {
    hovered = null;
    tooltip.classList.add("hidden");
    animate();
  });

  const resize = new ResizeObserver((entries) => {
    width = entries[0].contentRect.width;
    draw();
  });
  resize.observe(container);
  const theme = new MutationObserver(draw);
  theme.observe(document.documentElement, { attributes: true, attributeFilter: ["class"] });
  cleanups.push(() => {
    resize.disconnect();
    theme.disconnect();
    if (frame !== null) cancelAnimationFrame(frame);
  });
  draw();
}

async function load() {
  const response = await fetch(`/status/${encodeURIComponent(PAGE)}`);
  if (!response.ok) {
    document.getElementById("header").innerHTML = `<div class="rounded-2xl border p-8 text-center text-muted-foreground text-sm">Status page "${esc(PAGE)}" is not available (${response.status}).</div>`;
    return;
  }

  const page = await response.json();
  document.title = page.title;
  document.getElementById("title").textContent = page.title;
  document.getElementById("site-name").textContent = page.title;
  document.getElementById("description").textContent = page.description ?? "";
  document.getElementById("header").innerHTML = renderHeader(computeOverallStatus(page.components));

  const groups = new Map();
  page.components.forEach((component, index) => {
    const key = component.group ?? "";
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push({ component, index });
  });

  cleanups.forEach((cleanup) => cleanup());
  cleanups = [];
  document.getElementById("groups").innerHTML = page.components.length === 0
    ? `<div class="rounded-2xl border p-8 text-center text-muted-foreground text-sm">No services configured yet.</div>`
    : [...groups].map(([group, items]) => `
      <section class="flex flex-col gap-3">
        ${group ? `<h2 class="text-sm font-semibold text-muted-foreground uppercase tracking-wide">${esc(group)}</h2>` : ""}
        ${items.map(({ component, index }) => renderRow(component, index)).join("")}
      </section>`).join("");

  document.querySelectorAll("[data-bar]").forEach((el) => {
    mountBar(el, page.components[Number(el.dataset.bar)].history);
  });

  document.getElementById("updated").textContent =
    `Last updated ${new Date(page.generatedAt).toLocaleString(undefined, { month: "short", day: "numeric", year: "numeric", hour: "numeric", minute: "2-digit" })}`;
}

// ThemeToggle: follows the system until toggled, then remembers the choice.
function applyTheme(dark) {
  document.documentElement.classList.toggle("dark", dark);
  document.getElementById("theme-toggle").innerHTML = svg(dark ? SUN : MOON, "size-4");
}
applyTheme(localStorage.getItem("theme") ? localStorage.getItem("theme") === "dark" : matchMedia("(prefers-color-scheme: dark)").matches);
document.getElementById("theme-toggle").addEventListener("click", () => {
  const dark = !document.documentElement.classList.contains("dark");
  localStorage.setItem("theme", dark ? "dark" : "light");
  applyTheme(dark);
});

// AutoRefresh.tsx
load();
setInterval(load, REFRESH_MS);
