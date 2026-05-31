// ============================================================
// Rescate Académico — Hero Shader
// A dependency-free WebGL flowing-gradient (the "shaders.com" look),
// themed from the brand CSS variables. Calm, slow, premium.
// Falls back silently to the CSS gradient if WebGL is unavailable
// or the user prefers reduced motion.
//
// Usage:
//   <div class="ra-hero">
//     <div class="ra-hero-canvas-wrap"><canvas class="ra-hero-shader"></canvas></div>
//     <div class="ra-hero-scrim"></div>
//     <div class="ra-hero-content"> ... </div>
//   </div>
// ============================================================
(function () {
    'use strict';

    var prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function hexToRgb(input) {
        if (!input) return null;
        var c = input.trim();
        // rgb()/rgba()
        var m = c.match(/rgba?\(([^)]+)\)/i);
        if (m) {
            var parts = m[1].split(',').map(function (s) { return parseFloat(s); });
            return [parts[0] / 255, parts[1] / 255, parts[2] / 255];
        }
        if (c[0] === '#') c = c.slice(1);
        if (c.length === 3) c = c.split('').map(function (x) { return x + x; }).join('');
        if (c.length < 6) return null;
        var n = parseInt(c.slice(0, 6), 16);
        return [((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255];
    }

    function cssVar(name, fallback) {
        var v = getComputedStyle(document.documentElement).getPropertyValue(name);
        return hexToRgb(v) || hexToRgb(fallback) || [0.06, 0.46, 0.43];
    }

    var VERT = 'attribute vec2 p; void main(){ gl_Position = vec4(p, 0.0, 1.0); }';

    // Flowing domain-warped gradient. Cheap enough for integrated GPUs.
    var FRAG = [
        'precision highp float;',
        'uniform vec2 u_res;',
        'uniform float u_time;',
        'uniform vec3 u_c1;',   // primary
        'uniform vec3 u_c2;',   // accent
        'uniform vec3 u_c3;',   // base/surface tint
        'uniform float u_dark;',
        // smooth value noise
        'float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1,311.7)))*43758.5453); }',
        'float noise(vec2 p){',
        '  vec2 i=floor(p); vec2 f=fract(p);',
        '  vec2 u=f*f*(3.0-2.0*f);',
        '  return mix(mix(hash(i),hash(i+vec2(1.0,0.0)),u.x),',
        '             mix(hash(i+vec2(0.0,1.0)),hash(i+vec2(1.0,1.0)),u.x),u.y);',
        '}',
        'float fbm(vec2 p){',
        '  float v=0.0; float a=0.5;',
        '  for(int i=0;i<4;i++){ v+=a*noise(p); p*=2.0; a*=0.5; }',
        '  return v;',
        '}',
        'void main(){',
        '  vec2 uv = gl_FragCoord.xy / u_res.xy;',
        '  vec2 asp = vec2(u_res.x/u_res.y, 1.0);',
        '  vec2 p = uv * asp * 2.2;',
        '  float t = u_time * 0.045;',
        '  vec2 q = vec2(fbm(p + vec2(0.0, t)), fbm(p + vec2(5.2, -t)));',
        '  vec2 r = vec2(fbm(p + 3.0*q + vec2(1.7, 9.2) + t*0.5),',
        '               fbm(p + 3.0*q + vec2(8.3, 2.8) - t*0.5));',
        '  float f = fbm(p + 2.5*r);',
        '  vec3 col = mix(u_c1, u_c2, smoothstep(0.15, 0.95, f));',
        '  col = mix(col, u_c3, smoothstep(0.55, 1.15, length(r)));',
        '  // subtle vignette toward the corners',
        '  float vig = smoothstep(1.25, 0.2, length(uv - 0.5));',
        '  col *= mix(0.82, 1.08, vig);',
        '  gl_FragColor = vec4(col, 1.0);',
        '}'
    ].join('\n');

    function compile(gl, type, src) {
        var s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
            console.warn('[RaHeroShader]', gl.getShaderInfoLog(s));
            return null;
        }
        return s;
    }

    function initCanvas(canvas) {
        var hero = canvas.closest('.ra-hero');
        var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
        if (!gl) return;

        var vs = compile(gl, gl.VERTEX_SHADER, VERT);
        var fs = compile(gl, gl.FRAGMENT_SHADER, FRAG);
        if (!vs || !fs) return;

        var prog = gl.createProgram();
        gl.attachShader(prog, vs);
        gl.attachShader(prog, fs);
        gl.linkProgram(prog);
        if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) return;
        gl.useProgram(prog);

        var buf = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, buf);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
        var loc = gl.getAttribLocation(prog, 'p');
        gl.enableVertexAttribArray(loc);
        gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);

        var uRes = gl.getUniformLocation(prog, 'u_res');
        var uTime = gl.getUniformLocation(prog, 'u_time');
        var uC1 = gl.getUniformLocation(prog, 'u_c1');
        var uC2 = gl.getUniformLocation(prog, 'u_c2');
        var uC3 = gl.getUniformLocation(prog, 'u_c3');
        var uDark = gl.getUniformLocation(prog, 'u_dark');

        function readColors() {
            var dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
            gl.uniform3fv(uC1, cssVar('--ra-primary', '#0f766e'));
            gl.uniform3fv(uC2, cssVar('--ra-accent', '#6c1d45'));
            gl.uniform3fv(uC3, cssVar('--ra-surface', dark ? '#0c0c0e' : '#ffffff'));
            gl.uniform1f(uDark, dark ? 1.0 : 0.0);
        }

        function resize() {
            var dpr = Math.min(window.devicePixelRatio || 1, 1.75);
            var w = canvas.clientWidth || hero.clientWidth;
            var h = canvas.clientHeight || hero.clientHeight;
            canvas.width = Math.max(1, Math.floor(w * dpr));
            canvas.height = Math.max(1, Math.floor(h * dpr));
            gl.viewport(0, 0, canvas.width, canvas.height);
            gl.uniform2f(uRes, canvas.width, canvas.height);
        }

        readColors();
        resize();
        window.addEventListener('resize', resize, { passive: true });

        // React to theme toggle
        new MutationObserver(readColors).observe(document.documentElement, {
            attributes: true, attributeFilter: ['data-bs-theme']
        });

        if (hero) hero.classList.add('is-shading');

        var start = performance.now();
        var raf = null;
        var visible = true;

        function frame(now) {
            if (prefersReduced) {
                // Render a single static frame, then stop.
                gl.uniform1f(uTime, 8.0);
                gl.drawArrays(gl.TRIANGLES, 0, 3);
                return;
            }
            gl.uniform1f(uTime, (now - start) / 1000);
            gl.drawArrays(gl.TRIANGLES, 0, 3);
            raf = requestAnimationFrame(frame);
        }

        // Pause when the hero scrolls off-screen (saves battery/GPU)
        if ('IntersectionObserver' in window) {
            new IntersectionObserver(function (entries) {
                entries.forEach(function (e) {
                    visible = e.isIntersecting;
                    if (visible && !prefersReduced && raf === null) {
                        raf = requestAnimationFrame(frame);
                    } else if (!visible && raf !== null) {
                        cancelAnimationFrame(raf);
                        raf = null;
                    }
                });
            }, { threshold: 0.01 }).observe(hero || canvas);
        }

        raf = requestAnimationFrame(frame);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('canvas.ra-hero-shader').forEach(initCanvas);
    });
})();
