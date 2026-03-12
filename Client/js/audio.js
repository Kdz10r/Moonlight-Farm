const AudioEngine = {
    ctx: null,
    sounds: {},
    isMuted: false,

    init() {
        this.ctx = new (window.AudioContext || window.webkitAudioContext)();
        this.createAmbient();
    },

    createAmbient() {
        // Simple procedural ambient sound (wind/crickets)
        const duration = 2;
        const sampleRate = this.ctx.sampleRate;
        const buffer = this.ctx.createBuffer(1, sampleRate * duration, sampleRate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < buffer.length; i++) {
            data[i] = Math.random() * 2 - 1;
        }

        this.sounds.whiteNoise = buffer;
    },

    playMusic(isNight) {
        if (!this.ctx || this.isMuted) return;
        
        // Simple procedural melody
        const playNote = (freq, delay, duration) => {
            const osc = this.ctx.createOscillator();
            const g = this.ctx.createGain();
            
            osc.type = 'sine';
            osc.frequency.setValueAtTime(freq, this.ctx.currentTime + delay);
            
            g.gain.setValueAtTime(0, this.ctx.currentTime + delay);
            g.gain.linearRampToValueAtTime(0.05, this.ctx.currentTime + delay + 0.1);
            g.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + delay + duration);
            
            osc.connect(g);
            g.connect(this.ctx.destination);
            
            osc.start(this.ctx.currentTime + delay);
            osc.stop(this.ctx.currentTime + delay + duration);
        };

        const scale = isNight ? [220, 261, 329, 392, 440] : [261, 293, 329, 392, 440, 523];
        
        const loopMelody = () => {
            if (this.isMuted) return;
            let time = 0;
            for (let i = 0; i < 8; i++) {
                const note = scale[Math.floor(Math.random() * scale.length)];
                playNote(note, time, 1.5);
                time += 2;
            }
            setTimeout(() => loopMelody(), time * 1000);
        };

        loopMelody();
    },

    playAmbient(isNight) {
        if (!this.ctx || this.isMuted) return;
        
        const source = this.ctx.createBufferSource();
        source.buffer = this.sounds.whiteNoise;
        
        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.value = isNight ? 400 : 800; // Lower frequency for night (wind)
        
        const gain = this.ctx.createGain();
        gain.gain.value = 0.02;

        source.connect(filter);
        filter.connect(gain);
        gain.connect(this.ctx.destination);
        
        source.loop = true;
        source.start();
        this.ambientSource = source;
    },

    playSound(type) {
        if (!this.ctx || this.isMuted) return;
        
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        
        osc.connect(gain);
        gain.connect(this.ctx.destination);
        
        switch(type) {
            case 'walk':
                osc.type = 'sine';
                osc.frequency.setValueAtTime(100, this.ctx.currentTime);
                osc.frequency.exponentialRampToValueAtTime(10, this.ctx.currentTime + 0.1);
                gain.gain.setValueAtTime(0.1, this.ctx.currentTime);
                gain.gain.linearRampToValueAtTime(0, this.ctx.currentTime + 0.1);
                osc.start();
                osc.stop(this.ctx.currentTime + 0.1);
                break;
            case 'tool':
                osc.type = 'square';
                osc.frequency.setValueAtTime(200, this.ctx.currentTime);
                osc.frequency.exponentialRampToValueAtTime(400, this.ctx.currentTime + 0.05);
                gain.gain.setValueAtTime(0.05, this.ctx.currentTime);
                gain.gain.linearRampToValueAtTime(0, this.ctx.currentTime + 0.1);
                osc.start();
                osc.stop(this.ctx.currentTime + 0.1);
                break;
            case 'ui':
                osc.type = 'sine';
                osc.frequency.setValueAtTime(800, this.ctx.currentTime);
                osc.frequency.exponentialRampToValueAtTime(1200, this.ctx.currentTime + 0.05);
                gain.gain.setValueAtTime(0.1, this.ctx.currentTime);
                gain.gain.linearRampToValueAtTime(0, this.ctx.currentTime + 0.1);
                osc.start();
                osc.stop(this.ctx.currentTime + 0.1);
                break;
        }
    }
};
