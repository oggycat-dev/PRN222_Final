/**
 * Enhanced SignalR Novels Page Integration - Clean Architecture
 * Handles real-time notifications for novel additions and updates
 */
class NovelsSignalR {
    constructor(config) {
        this.config = config;
        this.connection = null;
        this.connectionState = 'Disconnected';
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        
        console.log('🚀 Initializing NovelsSignalR with config:', config);
        this.initialize();
    }

    async initialize() {
        try {
            if (!this.config.enabled) {
                console.log('⏸️ SignalR disabled, skipping initialization');
                return;
            }

            console.log('🔧 Setting up SignalR connection to:', this.config.hubUrl);
            
            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.config.hubUrl)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.previousRetryCount < 3) {
                            return Math.random() * 10000;
                        } else {
                            return null; // Stop reconnecting
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupEventHandlers();
            
            // Connect
            await this.connect();
            
        } catch (error) {
            console.error('❌ Failed to initialize SignalR:', error);
        }
    }

    setupEventHandlers() {
        if (!this.connection) return;

        // Connection state handlers
        this.connection.onclose(error => {
            console.log('❌ SignalR connection closed:', error);
            this.connectionState = 'Disconnected';
        });

        this.connection.onreconnecting(error => {
            console.log('🔄 SignalR reconnecting:', error);
            this.connectionState = 'Reconnecting';
        });

        this.connection.onreconnected(connectionId => {
            console.log('✅ SignalR reconnected with ID:', connectionId);
            this.connectionState = 'Connected';
            this.rejoinGroups();
        });

        // Novel-specific event handlers
        this.connection.on('NovelAdded', (notification) => {
            console.log('📚 Received NovelAdded notification:', notification);
            this.handleNovelAdded(notification);
        });

        this.connection.on('NovelUpdated', (notification) => {
            console.log('📝 Received NovelUpdated notification:', notification);
            this.handleNovelUpdated(notification);
        });

        this.connection.on('ChapterAdded', (notification) => {
            console.log('📖 Received ChapterAdded notification:', notification);
            this.handleChapterAdded(notification);
        });

        // Generic notification handler
        this.connection.on('GeneralNotification', (notification) => {
            console.log('🔔 Received general notification:', notification);
            this.showNotification(notification);
        });
    }

    async connect() {
        try {
            console.log('🔗 Attempting to connect to SignalR hub...');
            await this.connection.start();
            
            console.log('✅ SignalR connection established with ID:', this.connection.connectionId);
            this.connectionState = 'Connected';
            this.reconnectAttempts = 0;
            
            // Join notification group
            await this.joinNotificationGroup();
            
        } catch (error) {
            console.error('❌ Failed to connect to SignalR:', error);
            
            // Retry connection
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnectAttempts++;
                const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
                console.log(`🔄 Retrying connection in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
                
                setTimeout(() => this.connect(), delay);
            } else {
                console.error('❌ Max reconnection attempts reached');
            }
        }
    }

    async joinNotificationGroup() {
        try {
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                console.log('👥 Joining notification group...');
                await this.connection.invoke('JoinNotificationGroup');
                console.log('✅ Successfully joined notification group');
            }
        } catch (error) {
            console.error('❌ Failed to join notification group:', error);
        }
    }

    async rejoinGroups() {
        console.log('🔄 Rejoining groups after reconnection...');
        await this.joinNotificationGroup();
    }

    handleNovelAdded(notification) {
        console.log('📚 Processing novel added notification:', notification);
        
        // Show toast notification
        this.showNotification({
            title: 'Tiểu thuyết mới!',
            message: `"${notification.NovelTitle}" vừa được thêm vào thư viện`,
            type: 'success',
            icon: 'fas fa-book-plus'
        });

        // Add novel to grid if we're on the right page
        this.addNovelToGrid(notification);
    }

    handleNovelUpdated(notification) {
        console.log('📝 Processing novel updated notification:', notification);
        
        this.showNotification({
            title: 'Cập nhật tiểu thuyết',
            message: `"${notification.NovelTitle}" đã được cập nhật`,
            type: 'info',
            icon: 'fas fa-edit'
        });

        // Update novel in grid if exists
        this.updateNovelInGrid(notification);
    }

    handleChapterAdded(notification) {
        console.log('📖 Processing chapter added notification:', notification);
        
        this.showNotification({
            title: 'Chương mới!',
            message: `"${notification.NovelTitle}" có chương mới: ${notification.ChapterTitle}`,
            type: 'primary',
            icon: 'fas fa-plus-circle'
        });
    }

    showNotification(notification) {
        try {
            console.log('🔔 Showing notification:', notification);
            
            // Create notification content
            const notificationHtml = `
                <div class="d-flex align-items-center">
                    <div class="notification-icon me-3">
                        <i class="${notification.icon || 'fas fa-bell'} fa-lg text-${notification.type || 'primary'}"></i>
                    </div>
                    <div class="notification-content">
                        <div class="notification-title fw-bold">${notification.title}</div>
                        <div class="notification-message">${notification.message}</div>
                    </div>
                </div>
            `;

            // Get or create toast container
            let toastContainer = document.getElementById('notification-toast-container');
            if (!toastContainer) {
                toastContainer = document.createElement('div');
                toastContainer.id = 'notification-toast-container';
                toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
                toastContainer.style.zIndex = '9999';
                document.body.appendChild(toastContainer);
            }

            // Create toast element
            const toastId = `toast-${Date.now()}`;
            const toastElement = document.createElement('div');
            toastElement.id = toastId;
            toastElement.className = `toast show border-${notification.type || 'primary'}`;
            toastElement.setAttribute('role', 'alert');
            toastElement.innerHTML = `
                <div class="toast-header bg-${notification.type || 'primary'} text-white">
                    <strong class="me-auto">
                        <i class="${notification.icon || 'fas fa-bell'} me-2"></i>
                        Thông báo
                    </strong>
                    <small class="text-white-50">${new Date().toLocaleTimeString('vi-VN')}</small>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${notificationHtml}
                </div>
            `;

            // Add toast to container
            toastContainer.appendChild(toastElement);

            // Initialize Bootstrap toast
            const toast = new bootstrap.Toast(toastElement, {
                autohide: true,
                delay: 5000
            });

            // Show toast
            toast.show();

            // Remove toast element after it's hidden
            toastElement.addEventListener('hidden.bs.toast', () => {
                toastElement.remove();
            });

            console.log('✅ Notification displayed successfully');
            
        } catch (error) {
            console.error('❌ Failed to show notification:', error);
        }
    }

    addNovelToGrid(notification) {
        try {
            const novelsGrid = document.getElementById('novels-grid');
            if (!novelsGrid) {
                console.log('📍 Novels grid not found, skipping grid update');
                return;
            }

            // Check if we need to refresh the page or add the novel directly
            // For simplicity, we'll just refresh the page to get the latest data
            console.log('🔄 Refreshing page to show new novel...');
            
            // Add a small delay before refresh to let user see the notification
            setTimeout(() => {
                window.location.reload();
            }, 2000);
            
        } catch (error) {
            console.error('❌ Failed to add novel to grid:', error);
        }
    }

    updateNovelInGrid(notification) {
        try {
            console.log('🔄 Updating novel in grid:', notification);
            // For now, just refresh to get latest data
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        } catch (error) {
            console.error('❌ Failed to update novel in grid:', error);
        }
    }

    // Public method to check connection status
    getConnectionState() {
        return {
            state: this.connectionState,
            connectionId: this.connection ? this.connection.connectionId : null,
            isConnected: this.connection && this.connection.state === signalR.HubConnectionState.Connected
        };
    }

    // Public method to manually reconnect
    async reconnect() {
        if (this.connection) {
            await this.connection.stop();
        }
        this.reconnectAttempts = 0;
        await this.connect();
    }

    // Cleanup method
    async disconnect() {
        if (this.connection) {
            console.log('🔌 Disconnecting SignalR...');
            await this.connection.stop();
            this.connectionState = 'Disconnected';
        }
    }
}

// Export for use in other scripts
window.NovelsSignalR = NovelsSignalR;
