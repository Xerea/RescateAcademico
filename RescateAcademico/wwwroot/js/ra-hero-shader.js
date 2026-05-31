// ============================================================
// Rescate Académico — Hero Aurora
// A calm, premium, low-frequency flowing-gradient backdrop.
// Renderer preference: WebGPU → WebGL → (CSS fallback handled in CSS).
// Themed from brand CSS variables, re-themes on dark toggle,
// pauses off-screen, respects prefers-reduced-motion.
//
// The visual is intentionally SUBTLE: large soft bands that drift
// slowly — an aurora, not a plasma. No high-frequency noise.
// ============================================================
(function () {
    'use strict';

    var prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Skip the animated GPU layer on constrained devices — the calm CSS
    // gradient remains. Cheap heuristics, no libraries.
    function isLowEnd() {
        try {
            if (navigator.connection && navigator.connection.saveData) return true;
            if (typeof navigator.hardwareConcurrency === 'number' && navigator.hardwareConcurrency <= 4) return true;
            if (typeof navigator.deviceMemory === 'number' && navigator.deviceMemory <= 4) return true;
            if (window.matchMedia('(max-width: 767.98px)').matches) return true;
        } catch (e) { /* ignore */ }
        return false;
    }
    var skipAnimation = prefersReduced || isLowEnd();

    function srgb(input, fallback) {
        var c = (input || '').trim() || fallback;
        var m = c.match(/rgba?\(([^)]+)\)/i);
        if (m) {
            var p = m[1].split(',').map(parseFloat);
            return [p[0] / 255, p[1] / 255, p[2] / 255];
        }
        if (c[0] === '#') c = c.slice(1);
        if (c.length === 3) c = c.split('').map(function (x) { return x + x; }).join('');
        var n = parseInt(c.slice(0, 6), 16);
        if (isNaN(n)) { c = fallback.replace('#', ''); n = parseInt(c, 16); }
        return [((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255];
    }
    function brandColors() {
        var cs = getComputedStyle(document.documentElement);
        var dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
        return {
            c1: srgb(cs.getPropertyValue('--ra-primary'), '#0f766e'),
            c2: srgb(cs.getPropertyValue('--ra-accent'), '#6c1d45'),
            c3: srgb(cs.getPropertyValue('--ra-surface'), dark ? '#0c0c0e' : '#ffffff'),
            dark: dark ? 1 : 0
        };
    }

    // ---- Shared aurora math (GLSL + WGSL kept visually identical) ----
    // Two slow sine-warped bands of brand color over the surface tint.
    var GLSL_FRAG = [
        'precision highp float;',
        'uniform vec2 u_res; uniform float u_time;',
        'uniform vec3 u_c1; uniform vec3 u_c2; uniform vec3 u_c3;',
        'float band(vec2 uv, float off, float sp){',
        '  float y = uv.y + 0.18*sin(uv.x*2.2 + u_time*sp + off)',
        '                 + 0.10*sin(uv.x*4.5 - u_time*sp*0.6 + off*2.0);',
        '  return smoothstep(0.55, 0.0, abs(y - 0.5));',
        '}',
        'void main(){',
        '  vec2 uv = gl_FragCoord.xy / u_res.xy;',
        '  vec3 col = u_c3;',
        '  float b1 = band(uv, 0.0, 0.10);',
        '  float b2 = band(uv, 2.4, 0.07);',
        '  col = mix(col, u_c1, b1*0.42);',
        '  col = mix(col, u_c2, b2*0.34);',
        '  // gentle corner falloff',
        '  col *= 1.0 - 0.10*length(uv-0.5);',
        '  gl_FragColor = vec4(col, 1.0);',
        '}'
    ].join('\n');

    var WGSL = [
        'struct U { res: vec2<f32>, time: f32, _p: f32, c1: vec4<f32>, c2: vec4<f32>, c3: vec4<f32> };',
        '@group(0) @binding(0) var<uniform> u: U;',
        '@vertex fn vs(@builtin(vertex_index) i: u32) -> @builtin(position) vec4<f32> {',
        '  var p = array<vec2<f32>,3>(vec2(-1.0,-1.0), vec2(3.0,-1.0), vec2(-1.0,3.0));',
        '  return vec4<f32>(p[i], 0.0, 1.0);',
        '}',
        'fn band(uv: vec2<f32>, off: f32, sp: f32) -> f32 {',
        '  let y = uv.y + 0.18*sin(uv.x*2.2 + u.time*sp + off) + 0.10*sin(uv.x*4.5 - u.time*sp*0.6 + off*2.0);',
        '  return smoothstep(0.55, 0.0, abs(y - 0.5));',
        '}',
        '@fragment fn fs(@builtin(position) pos: vec4<f32>) -> @location(0) vec4<f32> {',
        '  let uv = vec2<f32>(pos.x / u.res.x, 1.0 - pos.y / u.res.y);',
        '  var col = u.c3.rgb;',
        '  let b1 = band(uv, 0.0, 0.10);',
        '  let b2 = band(uv, 2.4, 0.07);',
        '  col = mix(col, u.c1.rgb, b1*0.42);',
        '  col = mix(col, u.c2.rgb, b2*0.34);',
        '  col = col * (1.0 - 0.10*length(uv-vec2<f32>(0.5,0.5)));',
        '  return vec4<f32>(col, 1.0);',
        '}'
    ].join('\n');

    function setShading(canvas, on) {
        var hero = canvas.closest('.ra-hero');
        if (hero) hero.classList.toggle('is-shading', !!on);
    }
    function dpr() { return Math.min(window.devicePixelRatio || 1, 1.5); }

    // ---------------------- WebGPU ----------------------
    async function initWebGPU(canvas) {
        if (!('gpu' in navigator)) return false;
        var adapter = await navigator.gpu.requestAdapter();
        if (!adapter) return false;
        var device = await adapter.requestDevice();
        var ctx = canvas.getContext('webgpu');
        if (!ctx) return false;

        var format = navigator.gpu.getPreferredCanvasFormat();
        ctx.configure({ device: device, format: format, alphaMode: 'opaque' });

        var module = device.createShaderModule({ code: WGSL });
        var pipeline = device.createRenderPipeline({
            layout: 'auto',
            vertex: { module: module, entryPoint: 'vs' },
            fragment: { module: module, entryPoint: 'fs', targets: [{ format: format }] },
            primitive: { topology: 'triangle-list' }
        });

        // uniform buffer: res(2) time(1) pad(1) c1(4) c2(4) c3(4) = 16 floats
        var ubuf = device.createBuffer({ size: 16 * 4, usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST });
        var bind = device.createBindGroup({
            layout: pipeline.getBindGroupLayout(0),
            entries: [{ binding: 0, resource: { buffer: ubuf } }]
        });
        var data = new Float32Array(16);

        function resize() {
            var d = dpr();
            canvas.width = Math.max(1, Math.floor((canvas.clientWidth || 1) * d));
            canvas.height = Math.max(1, Math.floor((canvas.clientHeight || 1) * d));
        }
        resize();
        window.addEventListener('resize', resize, { passive: true });
        setShading(canvas, true);

        var start = performance.now();
        var raf = null, visible = true;
        function frame(now) {
            var col = brandColors();
            var t = prefersReduced ? 6.0 : (now - start) / 1000;
            data[0] = canvas.width; data[1] = canvas.height; data[2] = t; data[3] = 0;
            data[4] = col.c1[0]; data[5] = col.c1[1]; data[6] = col.c1[2]; data[7] = 1;
            data[8] = col.c2[0]; data[9] = col.c2[1]; data[10] = col.c2[2]; data[11] = 1;
            data[12] = col.c3[0]; data[13] = col.c3[1]; data[14] = col.c3[2]; data[15] = 1;
            device.queue.writeBuffer(ubuf, 0, data);

            var enc = device.createCommandEncoder();
            var pass = enc.beginRenderPass({
                colorAttachments: [{
                    view: ctx.getCurrentTexture().createView(),
                    clearValue: { r: 0, g: 0, b: 0, a: 1 }, loadOp: 'clear', storeOp: 'store'
                }]
            });
            pass.setPipeline(pipeline);
            pass.setBindGroup(0, bind);
            pass.draw(3);
            pass.end();
            device.queue.submit([enc.finish()]);

            if (prefersReduced) return;
            if (visible) raf = requestAnimationFrame(frame);
        }
        observeVisibility(canvas, function (v) {
            visible = v;
            if (v && !prefersReduced && raf === null) raf = requestAnimationFrame(frame);
            else if (!v && raf !== null) { cancelAnimationFrame(raf); raf = null; }
        });
        raf = requestAnimationFrame(frame);
        return true;
    }

    // ---------------------- WebGL fallback ----------------------
    function initWebGL(canvas) {
        var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
        if (!gl) return false;
        function sh(t, s) { var o = gl.createShader(t); gl.shaderSource(o, s); gl.compileShader(o); return gl.getShaderParameter(o, gl.COMPILE_STATUS) ? o : null; }
        var vs = sh(gl.VERTEX_SHADER, 'attribute vec2 p; void main(){ gl_Position=vec4(p,0.0,1.0); }');
        var fs = sh(gl.FRAGMENT_SHADER, GLSL_FRAG);
        if (!vs || !fs) return false;
        var prog = gl.createProgram(); gl.attachShader(prog, vs); gl.attachShader(prog, fs); gl.linkProgram(prog);
        if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) return false;
        gl.useProgram(prog);
        var buf = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER, buf);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
        var loc = gl.getAttribLocation(prog, 'p'); gl.enableVertexAttribArray(loc); gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
        var uRes = gl.getUniformLocation(prog, 'u_res'), uTime = gl.getUniformLocation(prog, 'u_time');
        var uC1 = gl.getUniformLocation(prog, 'u_c1'), uC2 = gl.getUniformLocation(prog, 'u_c2'), uC3 = gl.getUniformLocation(prog, 'u_c3');
        function resize() {
            var d = dpr();
            canvas.width = Math.max(1, Math.floor((canvas.clientWidth || 1) * d));
            canvas.height = Math.max(1, Math.floor((canvas.clientHeight || 1) * d));
            gl.viewport(0, 0, canvas.width, canvas.height);
            gl.uniform2f(uRes, canvas.width, canvas.height);
        }
        function colors() { var c = brandColors(); gl.uniform3fv(uC1, c.c1); gl.uniform3fv(uC2, c.c2); gl.uniform3fv(uC3, c.c3); }
        resize(); colors();
        window.addEventListener('resize', resize, { passive: true });
        new MutationObserver(colors).observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
        setShading(canvas, true);
        var start = performance.now(); var raf = null, visible = true;
        function frame(now) {
            gl.uniform1f(uTime, prefersReduced ? 6.0 : (now - start) / 1000);
            gl.drawArrays(gl.TRIANGLES, 0, 3);
            if (prefersReduced) return;
            if (visible) raf = requestAnimationFrame(frame);
        }
        observeVisibility(canvas, function (v) {
            visible = v;
            if (v && !prefersReduced && raf === null) raf = requestAnimationFrame(frame);
            else if (!v && raf !== null) { cancelAnimationFrame(raf); raf = null; }
        });
        raf = requestAnimationFrame(frame);
        return true;
    }

    function observeVisibility(canvas, cb) {
        var hero = canvas.closest('.ra-hero') || canvas;
        if (!('IntersectionObserver' in window)) { cb(true); return; }
        new IntersectionObserver(function (entries) {
            entries.forEach(function (e) { cb(e.isIntersecting); });
        }, { threshold: 0.01 }).observe(hero);
    }

    async function init(canvas) {
        // Constrained device → keep the calm CSS gradient, skip GPU work entirely.
        if (skipAnimation) return;
        try {
            if (await initWebGPU(canvas)) return;
        } catch (e) { /* fall through */ }
        try {
            if (initWebGL(canvas)) return;
        } catch (e) { /* CSS fallback remains */ }
        // else: the CSS gradient under .ra-hero-canvas-wrap stays visible.
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('canvas.ra-hero-shader').forEach(init);
    });
})();
