const fs = require("fs");
const http = require("http");
const path = require("path");
const { chromium } = require("playwright");

async function main() {
  const pdfPath = path.resolve(process.argv[2]);
  const outDir = path.resolve(process.argv[3]);
  const scale = Number(process.argv[4] || "1.65");
  fs.mkdirSync(outDir, { recursive: true });

  const nodeModules = process.env.NODE_PATH || path.join(
    process.env.USERPROFILE,
    ".cache",
    "codex-runtimes",
    "codex-primary-runtime",
    "dependencies",
    "node",
    "node_modules"
  );
  const pdfjsPath = path.join(nodeModules, "pdfjs-dist", "build", "pdf.mjs");
  const workerPath = path.join(nodeModules, "pdfjs-dist", "build", "pdf.worker.mjs");
  const pdfBytes = fs.readFileSync(pdfPath);

  const server = http.createServer((req, res) => {
    if (req.url === "/") {
      res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
      res.end(`<!doctype html><html><head><meta charset="utf-8">
        <style>
          body { margin: 0; background: #d8dce2; font-family: Arial, sans-serif; }
          .page-wrap { padding: 24px; }
          canvas { display: block; background: white; margin: 0 auto 24px auto; box-shadow: 0 2px 12px rgba(0,0,0,.18); }
        </style></head><body><div id="pages" class="page-wrap"></div></body></html>`);
      return;
    }
    if (req.url === "/pdf.mjs") {
      res.writeHead(200, { "Content-Type": "text/javascript" });
      res.end(fs.readFileSync(pdfjsPath));
      return;
    }
    if (req.url === "/pdf.worker.mjs") {
      res.writeHead(200, { "Content-Type": "text/javascript" });
      res.end(fs.readFileSync(workerPath));
      return;
    }
    if (req.url === "/document.pdf") {
      res.writeHead(200, { "Content-Type": "application/pdf" });
      res.end(pdfBytes);
      return;
    }
    res.writeHead(404);
    res.end("Not found");
  });
  await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
  const port = server.address().port;

  const edgePath = process.env.EDGE_PATH || "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
  const launchOptions = fs.existsSync(edgePath)
    ? { headless: true, executablePath: edgePath, args: ["--allow-file-access-from-files"] }
    : { headless: true, args: ["--allow-file-access-from-files"] };
  const browser = await chromium.launch(launchOptions);
  const page = await browser.newPage({ viewport: { width: 1400, height: 1900 }, deviceScaleFactor: 1 });
  await page.goto(`http://127.0.0.1:${port}/`);

  const count = await page.evaluate(async ({ scale }) => {
    const pdfjsLib = await import("/pdf.mjs");
    pdfjsLib.GlobalWorkerOptions.workerSrc = "/pdf.worker.mjs";
    const response = await fetch("/document.pdf");
    const buffer = await response.arrayBuffer();
    const bytes = new Uint8Array(buffer);
    const pdf = await pdfjsLib.getDocument({ data: bytes }).promise;
    const container = document.getElementById("pages");
    for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
      const pdfPage = await pdf.getPage(pageNum);
      const viewport = pdfPage.getViewport({ scale });
      const canvas = document.createElement("canvas");
      canvas.dataset.page = String(pageNum);
      canvas.width = Math.floor(viewport.width);
      canvas.height = Math.floor(viewport.height);
      const context = canvas.getContext("2d", { alpha: false });
      await pdfPage.render({ canvasContext: context, viewport }).promise;
      container.appendChild(canvas);
    }
    return pdf.numPages;
  }, { scale });

  for (let pageNum = 1; pageNum <= count; pageNum++) {
    const canvas = await page.locator(`canvas[data-page="${pageNum}"]`).elementHandle();
    await canvas.screenshot({ path: path.join(outDir, `page-${pageNum}.png`) });
  }
  await browser.close();
  server.close();
  console.log(count);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
