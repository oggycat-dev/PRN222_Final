// SignalR Notifications for Novels Page
class NovelsSignalR {
    constructor(config) {
        this.config = config;
        this.connection = null;
        this.isEnabled = config.enabled;
        
        if (this.isEnabled) {
            this.initialize();
        }
    }

    initialize() {
        console.log('🚀 Initializing SignalR for Novels page');
        
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.config.hubUrl)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupEventHandlers();
        this.startConnection();
    }

    setupEventHandlers() {
        // Listen for notifications
        this.connection.on('NewNotification', (notification) => {
            console.log('📩 Received notification:', notification);
            this.handleNotification(notification);
        });

        // Connection state handlers
        this.connection.onclose(error => {
            console.log(error ? '❌ Connection closed with error:' : '📴 Connection closed', error);
        });

        this.connection.onreconnecting(error => {
            console.log('🔄 Reconnecting...', error);
        });

        this.connection.onreconnected(connectionId => {
            console.log('✅ Reconnected:', connectionId);
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log('✅ Connected to SignalR hub');
            console.log('📋 Connection ID:', this.connection.connectionId);
        } catch (err) {
            console.error('❌ SignalR connection failed:', err);
        }
    }

    handleNotification(notification) {
        // Show toast notification
        this.showToast(notification);
        
        // Handle specific notification types
        switch (notification.Type) {
            case 'NovelAdded':
                this.handleNovelAdded(notification);
                break;
            case 'ChapterAdded':
                this.handleChapterAdded(notification);
                break;
            case 'NovelUpdated':
                this.handleNovelUpdated(notification);
                break;
        }
    }

    showToast(notification) {
        const toast = document.getElementById('notification-toast');
        if (!toast) {
            console.error('❌ Toast element not found');
            return;
        }

        const toastIcon = document.getElementById('toast-icon');
        const toastTitle = document.getElementById('toast-title');
        const toastMessage = document.getElementById('toast-message');

        // Set icon based on type
        const iconMap = {
            'NovelAdded': 'fas fa-book text-success me-2',
            'ChapterAdded': 'fas fa-plus-circle text-info me-2',
            'NovelUpdated': 'fas fa-edit text-warning me-2'
        };
        
        toastIcon.className = iconMap[notification.Type] || 'fas fa-bell text-primary me-2';
        toastTitle.textContent = notification.Title;
        toastMessage.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="flex-grow-1">
                    <p class="mb-1">${notification.Message}</p>
                    ${notification.NovelId ? '<small class="text-muted">Nhấn để xem sách</small>' : ''}
                </div>
            </div>
        `;

        // Add click handler
        if (notification.NovelId) {
            toast.style.cursor = 'pointer';
            toast.onclick = () => window.location.href = `/NovelDetails?id=${notification.NovelId}`;
        }

        // Show toast
        const bsToast = new bootstrap.Toast(toast, this.config.toastConfig);
        bsToast.show();
    }

    handleNovelAdded(notification) {
        console.log('📚 Novel added:', notification.NovelTitle);
        // Simple approach: refresh page after a short delay
        setTimeout(() => {
            console.log('🔄 Refreshing page to show new novel...');
            window.location.reload();
        }, 2000);
    }

    handleChapterAdded(notification) {
        console.log('📖 Chapter added:', notification.ChapterTitle);
        // Could implement chapter count update here
    }

    handleNovelUpdated(notification) {
        console.log('📝 Novel updated:', notification.NovelTitle);
        // Could implement novel update here
    }
}

// Export for use in other scripts
window.NovelsSignalR = NovelsSignalR;
