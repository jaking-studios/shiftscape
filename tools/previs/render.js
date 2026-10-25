// Renders stills and frame sequences from scene.html with headless Chromium.
const { chromium } = require('playwright');
const http = require('http'), fs = require('fs'), path = require('path');
const root = __dirname;
const out = process.argv[2] || path.join(root, 'out');
fs.mkdirSync(out, { recursive: true });

const server = http.createServer((req, res) => {
  const p = path.join(root, decodeURIComponent(req.url.split('?')[0]));
  if (!p.startsWith(root) || !fs.existsSync(p) || fs.statSync(p).isDirectory()) { res.writeHead(404); return res.end(); }
  res.writeHead(200, { 'Content-Type': p.endsWith('.js') ? 'text/javascript' : 'text/html' });
  fs.createReadStream(p).pipe(res);
});

(async () => {
  await new Promise(r => server.listen(0, r));
  const port = server.address().port;
  const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
  const stills = [
    ['hero',     'world=foundry&mode=hop&cam=hero&frame=40&dur=6&z0=10&z1=70&hat=crown&cape=1', 1600, 900],
    ['frost',    'world=frost&mode=crumble&frame=70&dur=6&z0=14&z1=60&suit=%2359e3b0&visor=%2314324a', 1600, 900],
    ['neon',     'world=neon&mode=memory&frame=55&dur=6&z0=14&z1=34&suit=%238a5cff&visor=%23ffb3f0&trail=%23ff2bd6', 1600, 900],
    ['temple',   'world=temple&mode=vault&frame=100&dur=6&z0=14&z1=40&suit=%23f4c542&visor=%233a2a00', 1600, 900],
    ['spire',    'world=spire&mode=spinner&frame=80&dur=6&z0=14&z1=45&suit=%23ff6a3d&visor=%23ffe36e', 1600, 900],
    ['gravity',  'world=frost&mode=gravity&frame=28&dur=6&z0=14&z1=50&suit=%23ff8fc8&visor=%23ffffff&toast=GRAVITY%20FLIP', 1600, 900],
    ['void',     'world=foundry&mode=void&cam=front&frame=100&dur=6&z0=14&z1=70&hat=crown&cape=1', 1600, 900],
    ['mobile',   'world=temple&mode=bounce&frame=60&dur=6&z0=14&z1=50&touch=1&suit=%23ff8fc8&visor=%23ffffff', 1280, 600],
  ];
  for (const [name, qs, w, h] of stills) {
    const page = await browser.newPage({ viewport: { width: w, height: h } });
    await page.goto(`http://127.0.0.1:${port}/scene.html?${qs}`);
    await page.waitForFunction(() => window.ready === true, null, { timeout: 60000 });
    await page.waitForTimeout(200);
    await page.screenshot({ path: path.join(out, name + '.png') });
    console.log('still', name);
    await page.close();
  }
  // Animation frames: three shots stitched (foundry hop → neon gravity → void chase)
  const clips = [
    ['world=foundry&mode=hop&dur=4&z0=10&z1=60&hat=crown&cape=1', 4],
    ['world=frost&mode=gravity&dur=4&z0=12&z1=52&suit=%23ff8fc8&visor=%23ffffff&trail=%23ff6b9a', 4],
    ['world=spire&mode=spinner&dur=3&z0=14&z1=44&suit=%23ff6a3d&visor=%23ffe36e', 3],
    ['world=foundry&mode=void&cam=front&dur=4&z0=14&z1=70&hat=crown&cape=1', 4],
  ];
  const fdir = path.join(out, 'frames'); fs.mkdirSync(fdir, { recursive: true });
  let n = 0;
  for (const [qs] of clips) {
    const page = await browser.newPage({ viewport: { width: 960, height: 540 } });
    await page.goto(`http://127.0.0.1:${port}/scene.html?${qs}`);
    await page.waitForFunction(() => window.ready === true, null, { timeout: 60000 });
    const frames = await page.evaluate(() => window.frames);
    for (let f = 0; f < frames; f++) {
      await page.evaluate(f => window.renderFrame(f), f);
      await page.screenshot({ path: path.join(fdir, String(n++).padStart(5, '0') + '.png') });
    }
    console.log('clip done', n);
    await page.close();
  }
  await browser.close(); server.close();
})().catch(e => { console.error(e); process.exit(1); });
