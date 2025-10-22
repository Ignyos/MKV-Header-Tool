// Pure JavaScript Frontend - MAUI Template with C# Backend Bridge

console.log('Pure JavaScript Frontend - Loading...');

/**
 * Frontend Bridge Interface - Clean abstraction for backend communication
 * This interface can be easily swapped for different backends (Web API, SignalR, etc.)
 */
class AppBridge {
    constructor() {
        this.bridgeService = null;
        this.isInitialized = false;
        this.lastUsedDirKey = 'mkvtool:lastUsedDirectory';
        console.log('Bridge interface initialized');
    }

    /**
     * Extract directory from a file path (supports Windows and POSIX)
     * @param {string} filePath
     * @returns {string|null}
     */
    getDirectoryFromPath(filePath) {
        if (!filePath || typeof filePath !== 'string') return null;
        const lastSlash = Math.max(filePath.lastIndexOf('\\'), filePath.lastIndexOf('/'));
        if (lastSlash <= 0) return null;
        return filePath.substring(0, lastSlash);
    }

    /**
     * Initialize the bridge with the C# JSInterop service
     * @param {object} dotNetBridgeService - .NET object reference for JSInterop
     */
    initialize(dotNetBridgeService) {
        this.bridgeService = dotNetBridgeService;
        this.isInitialized = true;
        console.log('Bridge connected to .NET backend');
        
        // Update UI to show bridge is ready
        this.updateConnectionStatus(true);
    }

    /**
     * Update connection status in the UI
     * @param {boolean} connected - Whether bridge is connected
     */
    updateConnectionStatus(connected) {
        // This could update a status indicator in the UI
        console.log(`Bridge status: ${connected ? 'Connected' : 'Disconnected'}`);
    }

    /**
     * Get application information
     * @returns {Promise<object>} App info object
     */
    async getAppInfo() {
        if (!this.isInitialized) {
            throw new Error('Bridge not initialized');
        }
        
        try {
            const result = await this.bridgeService.invokeMethodAsync('GetAppInfoAsync');
            return JSON.parse(result);
        } catch (error) {
            console.error('Get app info failed:', error);
            throw error;
        }
    }

    // MKV-specific bridge methods

    /**
     * Get available MKV properties from mkvpropedit
     * @returns {Promise<Array>} Array of available MKV properties
     */
    async getAvailableMkvProperties() {
        if (!this.isInitialized) {
            throw new Error('Bridge not initialized');
        }
        
        try {
            const result = await this.bridgeService.invokeMethodAsync('GetAvailableMkvPropertiesAsync');
            return JSON.parse(result);
        } catch (error) {
            console.error('Get available MKV properties failed:', error);
            throw error;
        }
    }

    /**
     * Read MKV file properties
     * @param {string} filePath - Path to the MKV file
     * @returns {Promise<object>} MKV file information
     */
    async readMkvFile(filePath) {
        if (!this.isInitialized) {
            throw new Error('Bridge not initialized');
        }
        
        try {
            // Persist last used directory in browser storage proactively
            const dir = this.getDirectoryFromPath(filePath);
            if (dir) {
                try { window.localStorage.setItem(this.lastUsedDirKey, dir); } catch {}
            }

            const result = await this.bridgeService.invokeMethodAsync('ReadMkvFileAsync', filePath);
            return JSON.parse(result);
        } catch (error) {
            console.error('Read MKV file failed:', error);
            throw error;
        }
    }

    /**
     * Validate if a file is a valid MKV file
     * @param {string} filePath - Path to the file to validate
     * @returns {Promise<boolean>} True if valid MKV file
     */
    async isValidMkvFile(filePath) {
        if (!this.isInitialized) {
            throw new Error('Bridge not initialized');
        }
        
        try {
            return await this.bridgeService.invokeMethodAsync('IsValidMkvFileAsync', filePath);
        } catch (error) {
            console.error('MKV file validation failed:', error);
            throw error;
        }
    }

    /**
     * Apply changes to an MKV file
     * @param {string} filePath - Path to the MKV file
     * @param {Array} changes - Array of property changes
     * @returns {Promise<object>} Edit result
     */
    async applyMkvChanges(filePath, changes) {
        if (!this.isInitialized) {
            throw new Error('Bridge not initialized');
        }
        
        try {
            const changesJson = JSON.stringify(changes);
            const result = await this.bridgeService.invokeMethodAsync('ApplyMkvChangesAsync', filePath, changesJson);
            return JSON.parse(result);
        } catch (error) {
            console.error('Apply MKV changes failed:', error);
            throw error;
        }
    }

    /**
     * Native pickers via backend bridge
     */
    async pickMkvFiles() {
        if (!this.isInitialized) throw new Error('Bridge not initialized');
        try {
            const json = await this.bridgeService.invokeMethodAsync('PickMkvFilesAsync');
            const items = JSON.parse(json);
            const normalized = Array.isArray(items) ? items.map(x => (
                typeof x === 'string' ? { FullPath: x, FileName: x.split(/[\\/]/).pop() } : x
            )) : [];
            if (normalized.length) {
                const dir = this.getDirectoryFromPath(normalized[0].FullPath);
                if (dir) { try { window.localStorage.setItem(this.lastUsedDirKey, dir); } catch {} }
            }
            return normalized;
        } catch (e) {
            console.error('pickMkvFiles failed', e);
            return [];
        }
    }

    async pickMkvFolder() {
        if (!this.isInitialized) throw new Error('Bridge not initialized');
        try {
            const json = await this.bridgeService.invokeMethodAsync('PickMkvFolderAsync');
            const items = JSON.parse(json);
            const normalized = Array.isArray(items) ? items.map(x => (
                typeof x === 'string' ? { FullPath: x, FileName: x.split(/[\\/]/).pop() } : x
            )) : [];
            if (normalized.length) {
                const dir = this.getDirectoryFromPath(normalized[0].FullPath);
                if (dir) { try { window.localStorage.setItem(this.lastUsedDirKey, dir); } catch {} }
            }
            return normalized;
        } catch (e) {
            console.error('pickMkvFolder failed', e);
            return [];
        }
    }

    /**
     * Get the last used directory for file picking
     * @returns {Promise<string|null>} Last used directory path
     */
    async getLastUsedDirectory() {
        // Prefer browser storage for persistence across sessions
        try {
            const value = window.localStorage.getItem(this.lastUsedDirKey);
            return value || null;
        } catch (storageError) {
            console.warn('localStorage unavailable; attempting backend fallback', storageError);
        }
    }

    /**
     * Set the last used directory for file picking
     * @param {string} directory - Directory path to remember
     */
    async setLastUsedDirectory(directory) {
        // Store in browser first
        try {
            if (directory) {
                window.localStorage.setItem(this.lastUsedDirKey, directory);
            } else {
                window.localStorage.removeItem(this.lastUsedDirKey);
            }
            return; // Success with local storage, no need to call backend
        } catch (storageError) {
            console.warn('localStorage unavailable; attempting backend fallback', storageError);
        }
    }
}

// Global bridge instance
window.appBridge = new AppBridge();

/**
 * Initialize the bridge (called from Blazor)
 * @param {object} dotNetBridgeService - .NET object reference
 */
window.initializeBridge = function(dotNetBridgeService) {
    console.log('Bridge initialization called from .NET');
    
    // Ensure DOM is ready before initializing bridge
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.appBridge.initialize(dotNetBridgeService);
            exposeBridgeMethods();
        });
    } else {
        window.appBridge.initialize(dotNetBridgeService);
        exposeBridgeMethods();
    }
};

function exposeBridgeMethods() {
    // Expose bridge methods globally for easy access
    window.bridgeService = {
        // General bridge methods
        getAppInfo: () => window.appBridge.getAppInfo(),
        
        // MKV-specific bridge methods
        getAvailableMkvProperties: () => window.appBridge.getAvailableMkvProperties(),
        readMkvFile: (filePath) => window.appBridge.readMkvFile(filePath),
        isValidMkvFile: (filePath) => window.appBridge.isValidMkvFile(filePath),
        applyMkvChanges: (filePath, changes) => window.appBridge.applyMkvChanges(filePath, changes),
        pickMkvFiles: () => window.appBridge.pickMkvFiles(),
        pickMkvFolder: () => window.appBridge.pickMkvFolder()
    };
    
    console.log('Global bridge methods exposed (including MKV methods)');
}

/**
 * UI Helper Functions
 */
function showLoading() {
    const loading = document.getElementById('loading');
    const resultDisplay = document.getElementById('result-display');
    if (loading) loading.classList.remove('hidden');
    if (resultDisplay) resultDisplay.classList.add('hidden');
    
    // Disable all buttons
    const buttons = document.querySelectorAll('.bridge-demo button, .panel-header button');
    buttons.forEach(btn => btn.disabled = true);
}

function hideLoading() {
    const loading = document.getElementById('loading');
    if (loading) loading.classList.add('hidden');
    
    // Re-enable all buttons
    const buttons = document.querySelectorAll('.bridge-demo button, .panel-header button');
    buttons.forEach(btn => btn.disabled = false);
}

function showResult(data) {
    const resultDisplay = document.getElementById('result-display');
    const resultContent = document.getElementById('result-content');
    
    if (resultDisplay && resultContent) {
        resultContent.textContent = JSON.stringify(data, null, 2);
        resultDisplay.classList.remove('hidden');
    }
}

async function getApplicationInfo() {
    showLoading();
    
    try {
        const result = await window.bridgeService.getAppInfo();
        showResult(result);
        console.log('App info retrieved:', result);
    } catch (error) {
        showResult({ error: error.message });
        console.error('Get app info failed:', error);
    } finally {
        hideLoading();
    }
}

/**
 * MKV-specific test functions
 */

// HARDCODED TEST FILE PATH - Change this to an actual MKV file path on your system
const TEST_MKV_FILE = "C:\\Users\\Nathan\\Desktop\\test.mkv"; // Update this path to an actual MKV file

async function testMkvProperties() {
    showLoading();
    
    try {
        const result = await window.bridgeService.getAvailableMkvProperties();
        showResult(result);
        console.log('Available MKV properties:', result);
    } catch (error) {
        showResult({ error: error.message });
        console.error('Get MKV properties failed:', error);
    } finally {
        hideLoading();
    }
}

async function testReadMkvFile() {
    showLoading();
    
    try {
        console.log(`Testing with file: ${TEST_MKV_FILE}`);
        
        // First validate the file
        const isValid = await window.bridgeService.isValidMkvFile(TEST_MKV_FILE);
        console.log('File validation result:', isValid);
        
        if (!isValid) {
            showResult({ 
                error: `File is not valid or not found: ${TEST_MKV_FILE}`,
                suggestion: "Update TEST_MKV_FILE variable in app.js to point to an actual MKV file"
            });
            return;
        }
        
        // Read the file properties
        const result = await window.bridgeService.readMkvFile(TEST_MKV_FILE);
        showResult(result);
        console.log('MKV file info:', result);
        
        // Also save to info.json for inspection
        await saveToInfoJson(result);
        
    } catch (error) {
        showResult({ 
            error: error.message,
            testFile: TEST_MKV_FILE,
            suggestion: "Make sure the TEST_MKV_FILE path points to a valid MKV file"
        });
        console.error('Read MKV file failed:', error);
    } finally {
        hideLoading();
    }
}

async function testMkvValidation() {
    showLoading();
    
    try {
        const result = await window.bridgeService.isValidMkvFile(TEST_MKV_FILE);
        showResult({ 
            filePath: TEST_MKV_FILE,
            isValid: result,
            message: result ? "File is a valid MKV" : "File is not valid or not found"
        });
        console.log('MKV validation result:', result);
    } catch (error) {
        showResult({ error: error.message });
        console.error('MKV validation failed:', error);
    } finally {
        hideLoading();
    }
}

async function saveToInfoJson(data) {
    try {
        // Create a blob with the JSON data
        const jsonData = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonData], { type: 'application/json' });
        
        // Create download link
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'mkv-info.json';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        
        console.log('✅ MKV info saved to mkv-info.json');
    } catch (error) {
        console.error('Failed to save info.json:', error);
    }
}

/**
 * Initialize the pure JavaScript UI
 */
function initializeUI() {
    const appDiv = document.getElementById('app');
    if (!appDiv) {
        console.error('App container not found!');
        return;
    }
    console.log('Hydrating static UI...');

    // Wire up picker buttons and list rendering
    const pickFileBtn = document.getElementById('btnSelectFiles');
    const pickFolderBtn = document.getElementById('btnSelectFolder');
    const selectedFilesUl = document.getElementById('fileList');
    const selectedPathEl = document.getElementById('selected-file-path');
    const tracksWrapper = document.getElementById('tracks-wrapper');
    const tracksBody = document.getElementById('tracks-body');
    const lastDirDiv = null; // lastDir not shown in the new layout
    let selectedLi = null;   // track current selection in the list
    let lastLoadedPath = null; // track last loaded details path
    let loadSeq = 0;           // sequence to avoid race conditions
    let loadDebounceId = 0;    // debounce timer id

    function renderLastDir() { /* no-op in the two-panel layout */ }

    function renderFiles(items) {
        selectedFilesUl.innerHTML = '';
        // Clear selection and displayed path on each render
        clearSelectionAndPath();
        if (!Array.isArray(items) || !items.length) {
            const li = document.createElement('li');
            li.textContent = 'No files selected';
            li.dataset.placeholder = 'true';
            selectedFilesUl.appendChild(li);
            return;
        }
        for (const item of items) {
            const f = typeof item === 'string' ? item : item.FullPath;
            const name = typeof item === 'string' ? (f.split(/[\\/]/).pop()) : item.FileName;
            const li = document.createElement('li');
            li.textContent = name || f;
            li.dataset.fullpath = f;
            li.title = f; // show full path on hover
            selectedFilesUl.appendChild(li);
        }
    }

    function setSelectedLi(li) {
        if (!li || li.dataset.placeholder === 'true') return;
        if (selectedLi === li) return;
        if (selectedLi) selectedLi.classList.remove('selected');
        selectedLi = li;
        selectedLi.classList.add('selected');
        renderSelectedPath(selectedLi.dataset.fullpath || '');
    }

    function getSelectedPath() {
        return selectedLi && selectedLi.dataset.fullpath ? selectedLi.dataset.fullpath : null;
    }

    function renderSelectedPath(path) {
        if (!selectedPathEl) return;
        const text = path || '';
        selectedPathEl.textContent = text;
        selectedPathEl.title = text;
    }

    function clearTracks() {
        if (!tracksBody) return;
        tracksBody.innerHTML = '';
        const tr = document.createElement('tr');
        tr.className = 'placeholder';
        const td = document.createElement('td');
        td.colSpan = 7;
        td.textContent = 'No file selected';
        tr.appendChild(td);
        tracksBody.appendChild(tr);
        // Hide raw JSON panel if visible
        const resultDisplay = document.getElementById('result-display');
        if (resultDisplay && !resultDisplay.classList.contains('hidden')) {
            resultDisplay.classList.add('hidden');
        }
    }

    function clearSelectionAndPath() {
        // Remove any existing selected highlight
        if (selectedLi) {
            selectedLi.classList.remove('selected');
        }
        selectedLi = null;
        lastLoadedPath = null;
        renderSelectedPath('');
    }

    function requestLoadSelected(delayMs = 200) {
        if (loadDebounceId) {
            clearTimeout(loadDebounceId);
            loadDebounceId = 0;
        }
        loadDebounceId = setTimeout(loadSelectedInternal, delayMs);
    }

    async function loadSelectedInternal() {
        loadDebounceId = 0;
        const path = getSelectedPath();
        if (!path) return;
        if (path === lastLoadedPath) return; // no-op if already loaded
        const seq = ++loadSeq;
        showLoading();
        try {
            const info = await window.bridgeService.readMkvFile(path);
            if (seq !== loadSeq) return; // stale
            renderTracks(info);
            lastLoadedPath = path;
        } catch (err) {
            if (seq !== loadSeq) return; // stale
            // On error, show JSON result for debugging
            showResult({ error: err?.message || String(err), file: path });
        } finally {
            if (seq === loadSeq) hideLoading();
        }
    }

    async function handlePickFiles() {
        const files = await window.bridgeService.pickMkvFiles();
        if (Array.isArray(files) && files.length > 0) {
            clearTracks();
            clearSelectionAndPath();
            renderFiles(files);
            renderLastDir();
        }
    }

    async function handlePickFolder() {
        const files = await window.bridgeService.pickMkvFolder();
        if (Array.isArray(files) && files.length > 0) {
            clearTracks();
            clearSelectionAndPath();
            renderFiles(files);
            renderLastDir();
        }
    }

    if (pickFileBtn) pickFileBtn.addEventListener('click', handlePickFiles);
    if (pickFolderBtn) pickFolderBtn.addEventListener('click', handlePickFolder);

    // Event delegation for list interactions
    if (selectedFilesUl) {
        selectedFilesUl.addEventListener('click', (e) => {
            const li = e.target && e.target.closest('li');
            if (!li || !selectedFilesUl.contains(li)) return;
            if (li.dataset.placeholder === 'true') return;
            setSelectedLi(li);
            // Debounced load on single-click (helps avoid double-load on double-click)
            requestLoadSelected(200);
        });

        selectedFilesUl.addEventListener('dblclick', async (e) => {
            const li = e.target && e.target.closest('li');
            if (!li || !selectedFilesUl.contains(li)) return;
            if (li.dataset.placeholder === 'true') return;
            setSelectedLi(li);
            // Immediate load on double-click
            requestLoadSelected(0);
        });
    }
    renderLastDir();

    function renderTracks(info) {
        // Hide raw JSON display if visible
        const resultDisplay = document.getElementById('result-display');
        if (resultDisplay && !resultDisplay.classList.contains('hidden')) {
            resultDisplay.classList.add('hidden');
        }

        if (!tracksBody) return;
        tracksBody.innerHTML = '';

        const tracks = (info && (info.Tracks || info.tracks)) || [];
        if (!Array.isArray(tracks) || tracks.length === 0) {
            const tr = document.createElement('tr');
            tr.className = 'placeholder';
            const td = document.createElement('td');
            td.colSpan = 7;
            td.textContent = 'No tracks found';
            tr.appendChild(td);
            tracksBody.appendChild(tr);
            return;
        }

        for (const t of tracks) {
            const tr = document.createElement('tr');

            // Backend MkvFileInfo.Tracks (PascalCase) shape
            const id = t.TrackNumber ?? t.id ?? t.Id ?? '';
            const type = t.TrackType ?? t.type ?? t.Type ?? '';
            const langIetf = t.LanguageIetf ?? ((t.properties && t.properties.language_ietf) || '');
            const langLegacy = t.LanguageLegacy ?? ((t.properties && t.properties.language) || '');
            const enabled = t.IsEnabled ?? t.enabled ?? t.Enabled;
            const def = t.IsDefault ?? t.default ?? t.Default;
            const forced = t.IsForced ?? t.forced ?? t.Forced;

            const cells = [
                id,
                type,
                langIetf,
                langLegacy,
                (enabled === true ? 'Yes' : enabled === false ? 'No' : ''),
                (def === true ? 'Yes' : def === false ? 'No' : ''),
                (forced === true ? 'Yes' : forced === false ? 'No' : '')
            ];
            for (const val of cells) {
                const td = document.createElement('td');
                td.textContent = val == null ? '' : String(val);
                tr.appendChild(td);
            }
            tracksBody.appendChild(tr);
        }
    }
}

// App initialization
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM ready - Pure JavaScript frontend starting');
    
    // Initialize the UI first
    initializeUI();
    
    console.log('Waiting for .NET bridge initialization...');
    console.log('Frontend ready - awaiting bridge connection');
});