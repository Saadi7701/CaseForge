// CaseForge AI Global Scripts - Canvas Fog Particle Engine & Typewriter Animation

// 1. Canvas Fog Generator
document.addEventListener("DOMContentLoaded", () => {
    initFogBackground();
    initGlobalAnimations();
});

function initFogBackground() {
    const canvas = document.getElementById("fogCanvas");
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let width = (canvas.width = window.innerWidth);
    let height = (canvas.height = window.innerHeight);

    window.addEventListener("resize", () => {
        width = (canvas.width = window.innerWidth);
        height = (canvas.height = window.innerHeight);
    });

    // Fog Particle constructor
    class Particle {
        constructor() {
            this.reset();
        }

        reset() {
            this.x = Math.random() * width;
            this.y = Math.random() * height;
            this.vx = (Math.random() - 0.5) * 0.25; // slow drift
            this.vy = (Math.random() - 0.5) * 0.15;
            this.radius = Math.random() * 200 + 100;
            this.alpha = Math.random() * 0.12 + 0.02;
            this.color = 255; // White/gray fog
        }

        update() {
            this.x += this.vx;
            this.y += this.vy;

            // boundary check
            if (this.x < -this.radius || this.x > width + this.radius ||
                this.y < -this.radius || this.y > height + this.radius) {
                this.reset();
            }
        }

        draw() {
            ctx.save();
            ctx.globalAlpha = this.alpha;
            let gradient = ctx.createRadialGradient(
                this.x, this.y, 0,
                this.x, this.y, this.radius
            );
            gradient.addColorStop(0, `rgba(${this.color}, ${this.color}, ${this.color}, 0.5)`);
            gradient.addColorStop(0.5, `rgba(${this.color}, ${this.color}, ${this.color}, 0.2)`);
            gradient.addColorStop(1, 'rgba(255, 255, 255, 0)');
            ctx.fillStyle = gradient;
            ctx.beginPath();
            ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
            ctx.fill();
            ctx.restore();
        }
    }

    const particleCount = 25;
    const particles = [];
    for (let i = 0; i < particleCount; i++) {
        particles.push(new Particle());
    }

    function animate() {
        ctx.clearRect(0, 0, width, height);
        // Dark moody background overlay tint
        ctx.fillStyle = "rgba(9, 10, 11, 0.05)";
        ctx.fillRect(0, 0, width, height);

        particles.forEach(p => {
            p.update();
            p.draw();
        });
        requestAnimationFrame(animate);
    }

    animate();
}

// 2. Global GSAP Entrance Animations
function initGlobalAnimations() {
    if (typeof gsap !== "undefined") {
        gsap.from("header", {
            y: -50,
            opacity: 0,
            duration: 0.8,
            ease: "power2.out"
        });

        gsap.from("main > div", {
            opacity: 0,
            y: 20,
            duration: 0.6,
            delay: 0.2,
            stagger: 0.15,
            ease: "power2.out"
        });
    }
}

// 3. Typewriter Dialogue Engine
class Typewriter {
    constructor(element, speed = 30) {
        this.element = element;
        this.speed = speed;
        this.timeoutId = null;
    }

    type(text) {
        // Clear any running write
        if (this.timeoutId) {
            clearTimeout(this.timeoutId);
        }

        this.element.innerHTML = "";
        this.element.classList.add("typewriter-cursor");
        let i = 0;

        const write = () => {
            if (i < text.length) {
                this.element.innerHTML += text.charAt(i);
                i++;
                this.timeoutId = setTimeout(write, this.speed);
            } else {
                this.element.classList.remove("typewriter-cursor");
            }
        };

        write();
    }
}

window.Typewriter = Typewriter;
