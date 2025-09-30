// NumbatWallet Dashboard Widget System
class DashboardWidgetManager {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.gridstack = null;
        this.widgets = new Map();
        this.availableWidgets = new Map();
        this.options = {
            column: 12,
            cellHeight: 70,
            verticalMargin: 15,
            animate: true,
            resizable: {
                handles: 'e, se, s, sw, w'
            },
            draggable: {
                handle: '.widget-header'
            },
            ...options
        };

        this.init();
    }

    init() {
        // Initialize GridStack
        this.gridstack = GridStack.init(this.options, this.container);

        // Register available widget types
        this.registerDefaultWidgets();

        // Load saved layout or default
        this.loadLayout();

        // Set up event listeners
        this.setupEventListeners();
    }

    registerDefaultWidgets() {
        // KPI Widgets
        this.registerWidget({
            id: 'active-wallets',
            type: 'kpi',
            name: 'Active Wallets',
            icon: '👛',
            defaultSize: { w: 3, h: 2 },
            component: 'KpiWidget',
            props: {
                title: 'Active Wallets',
                valueKey: 'activeWallets',
                trend: 'up',
                color: 'primary'
            }
        });

        this.registerWidget({
            id: 'total-credentials',
            type: 'kpi',
            name: 'Total Credentials',
            icon: '🎫',
            defaultSize: { w: 3, h: 2 },
            component: 'KpiWidget',
            props: {
                title: 'Total Credentials',
                valueKey: 'totalCredentials',
                trend: 'up',
                color: 'success'
            }
        });

        this.registerWidget({
            id: 'verifications-today',
            type: 'kpi',
            name: 'Verifications Today',
            icon: '✅',
            defaultSize: { w: 3, h: 2 },
            component: 'KpiWidget',
            props: {
                title: 'Verifications Today',
                valueKey: 'verificationsToday',
                color: 'info'
            }
        });

        this.registerWidget({
            id: 'active-tenants',
            type: 'kpi',
            name: 'Active Tenants',
            icon: '🏢',
            defaultSize: { w: 3, h: 2 },
            component: 'KpiWidget',
            props: {
                title: 'Active Tenants',
                valueKey: 'activeTenants',
                color: 'warning'
            }
        });

        // Chart Widgets
        this.registerWidget({
            id: 'wallet-growth',
            type: 'chart',
            name: 'Wallet Growth',
            icon: '📈',
            defaultSize: { w: 6, h: 4 },
            component: 'ChartWidget',
            props: {
                title: 'Wallet Growth',
                chartType: 'line',
                dataKey: 'walletGrowth'
            }
        });

        this.registerWidget({
            id: 'credential-types',
            type: 'chart',
            name: 'Credential Distribution',
            icon: '🍩',
            defaultSize: { w: 4, h: 4 },
            component: 'ChartWidget',
            props: {
                title: 'Credential Types',
                chartType: 'doughnut',
                dataKey: 'credentialTypes'
            }
        });

        this.registerWidget({
            id: 'verification-activity',
            type: 'chart',
            name: 'Verification Activity',
            icon: '📊',
            defaultSize: { w: 8, h: 4 },
            component: 'ChartWidget',
            props: {
                title: 'Verification Activity',
                chartType: 'bar',
                dataKey: 'verificationActivity'
            }
        });

        // Table Widgets
        this.registerWidget({
            id: 'recent-wallets',
            type: 'table',
            name: 'Recent Wallets',
            icon: '📋',
            defaultSize: { w: 6, h: 5 },
            component: 'TableWidget',
            props: {
                title: 'Recently Created Wallets',
                dataKey: 'recentWallets',
                columns: ['user', 'created', 'status']
            }
        });

        this.registerWidget({
            id: 'tenant-activity',
            type: 'table',
            name: 'Tenant Activity',
            icon: '🏗️',
            defaultSize: { w: 6, h: 5 },
            component: 'TableWidget',
            props: {
                title: 'Tenant Activity',
                dataKey: 'tenantActivity',
                columns: ['tenant', 'wallets', 'credentials', 'lastActive']
            }
        });

        // Real-time Widgets
        this.registerWidget({
            id: 'live-feed',
            type: 'realtime',
            name: 'Live Activity Feed',
            icon: '🔴',
            defaultSize: { w: 4, h: 6 },
            component: 'LiveFeedWidget',
            props: {
                title: 'Live Activity',
                maxItems: 20
            }
        });

        this.registerWidget({
            id: 'system-health',
            type: 'monitor',
            name: 'System Health',
            icon: '💚',
            defaultSize: { w: 3, h: 3 },
            component: 'SystemHealthWidget',
            props: {
                title: 'System Health',
                metrics: ['cpu', 'memory', 'database', 'api']
            }
        });
    }

    registerWidget(config) {
        this.availableWidgets.set(config.id, config);
    }

    addWidget(widgetId, position = null) {
        const config = this.availableWidgets.get(widgetId);
        if (!config) {
            console.error(`Widget ${widgetId} not found`);
            return null;
        }

        const widget = this.createWidgetElement(config);

        // Add to grid
        const gridPosition = position || this.findEmptySpace(config.defaultSize);
        this.gridstack.addWidget(widget, {
            x: gridPosition.x,
            y: gridPosition.y,
            w: config.defaultSize.w,
            h: config.defaultSize.h,
            id: widgetId
        });

        // Store widget instance
        this.widgets.set(widgetId, {
            config,
            element: widget,
            instance: null
        });

        // Initialize widget component
        this.initializeWidgetComponent(widgetId, config);

        // Save layout
        this.saveLayout();

        return widget;
    }

    createWidgetElement(config) {
        const widget = document.createElement('div');
        widget.className = 'grid-stack-item';
        widget.setAttribute('data-widget-id', config.id);

        const content = document.createElement('div');
        content.className = 'grid-stack-item-content widget-container';

        content.innerHTML = `
            <div class="widget-header">
                <div class="widget-header-left">
                    <span class="widget-icon">${config.icon}</span>
                    <span class="widget-title">${config.name}</span>
                </div>
                <div class="widget-header-right">
                    <button class="widget-btn widget-refresh" onclick="dashboardManager.refreshWidget('${config.id}')">
                        <i class="fas fa-sync-alt"></i>
                    </button>
                    <button class="widget-btn widget-settings" onclick="dashboardManager.configureWidget('${config.id}')">
                        <i class="fas fa-cog"></i>
                    </button>
                    <button class="widget-btn widget-remove" onclick="dashboardManager.removeWidget('${config.id}')">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
            </div>
            <div class="widget-body" id="widget-body-${config.id}">
                <div class="widget-loading">
                    <div class="spinner"></div>
                </div>
            </div>
        `;

        widget.appendChild(content);
        return widget;
    }

    initializeWidgetComponent(widgetId, config) {
        // This would be replaced with Blazor component initialization
        const bodyElement = document.getElementById(`widget-body-${widgetId}`);

        // Simulate loading
        setTimeout(() => {
            bodyElement.innerHTML = this.renderWidgetContent(config);
            this.loadWidgetData(widgetId);
        }, 500);
    }

    renderWidgetContent(config) {
        switch (config.type) {
            case 'kpi':
                return `
                    <div class="kpi-widget">
                        <div class="kpi-value">--</div>
                        <div class="kpi-label">${config.props.title}</div>
                        <div class="kpi-trend ${config.props.trend || ''}">
                            <span class="trend-icon"></span>
                            <span class="trend-value">--</span>
                        </div>
                    </div>
                `;
            case 'chart':
                return `
                    <div class="chart-widget">
                        <canvas id="chart-${config.id}"></canvas>
                    </div>
                `;
            case 'table':
                return `
                    <div class="table-widget">
                        <table class="widget-table">
                            <thead></thead>
                            <tbody></tbody>
                        </table>
                    </div>
                `;
            case 'realtime':
                return `
                    <div class="feed-widget">
                        <div class="feed-items"></div>
                    </div>
                `;
            case 'monitor':
                return `
                    <div class="monitor-widget">
                        <div class="health-indicators"></div>
                    </div>
                `;
            default:
                return '<div>Widget type not supported</div>';
        }
    }

    removeWidget(widgetId) {
        const widget = this.widgets.get(widgetId);
        if (!widget) return;

        // Remove from grid
        this.gridstack.removeWidget(widget.element);

        // Clean up
        this.widgets.delete(widgetId);

        // Save layout
        this.saveLayout();
    }

    refreshWidget(widgetId) {
        const widget = this.widgets.get(widgetId);
        if (!widget) return;

        // Add loading state
        const body = document.getElementById(`widget-body-${widgetId}`);
        body.classList.add('refreshing');

        // Reload data
        this.loadWidgetData(widgetId);

        // Remove loading state
        setTimeout(() => {
            body.classList.remove('refreshing');
        }, 1000);
    }

    configureWidget(widgetId) {
        // Open widget configuration modal
        this.openConfigModal(widgetId);
    }

    loadWidgetData(widgetId) {
        // This would fetch real data from API
        // For now, simulate with mock data
        const widget = this.widgets.get(widgetId);
        if (!widget) return;

        // Trigger data load based on widget type
        switch (widget.config.type) {
            case 'kpi':
                this.loadKpiData(widgetId);
                break;
            case 'chart':
                this.loadChartData(widgetId);
                break;
            case 'table':
                this.loadTableData(widgetId);
                break;
            case 'realtime':
                this.startRealtimeFeed(widgetId);
                break;
            case 'monitor':
                this.loadMonitorData(widgetId);
                break;
        }
    }

    loadKpiData(widgetId) {
        // Simulate KPI data load
        const element = document.querySelector(`[data-widget-id="${widgetId}"] .kpi-value`);
        if (element) {
            element.textContent = Math.floor(Math.random() * 10000);
        }
    }

    loadChartData(widgetId) {
        // Initialize chart with Chart.js
        const canvas = document.getElementById(`chart-${widgetId}`);
        if (!canvas) return;

        const widget = this.widgets.get(widgetId);
        const chartType = widget.config.props.chartType;

        // Create chart based on type
        new Chart(canvas, {
            type: chartType,
            data: this.generateMockChartData(chartType),
            options: this.getChartOptions(chartType)
        });
    }

    generateMockChartData(type) {
        if (type === 'doughnut') {
            return {
                labels: ['Driver License', 'Passport', 'Student ID', 'Employee ID', 'Other'],
                datasets: [{
                    data: [45, 25, 15, 10, 5],
                    backgroundColor: [
                        '#4F46E5',
                        '#10B981',
                        '#F59E0B',
                        '#EF4444',
                        '#8B5CF6'
                    ]
                }]
            };
        } else {
            return {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                datasets: [{
                    label: 'Dataset',
                    data: [12, 19, 3, 5, 2, 3],
                    borderColor: '#4F46E5',
                    backgroundColor: 'rgba(79, 70, 229, 0.1)'
                }]
            };
        }
    }

    getChartOptions(type) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: type === 'doughnut',
                    position: 'bottom'
                }
            }
        };
    }

    findEmptySpace(size) {
        // Find first available space for widget
        return { x: 0, y: 0 };
    }

    saveLayout() {
        const layout = this.gridstack.save();
        localStorage.setItem('dashboard-layout', JSON.stringify(layout));
    }

    loadLayout() {
        const saved = localStorage.getItem('dashboard-layout');
        if (saved) {
            const layout = JSON.parse(saved);
            this.gridstack.load(layout);

            // Reinitialize widget components
            layout.forEach(item => {
                const config = this.availableWidgets.get(item.id);
                if (config) {
                    this.initializeWidgetComponent(item.id, config);
                }
            });
        } else {
            // Load default layout
            this.loadDefaultLayout();
        }
    }

    loadDefaultLayout() {
        // Add default widgets
        ['active-wallets', 'total-credentials', 'verifications-today', 'active-tenants'].forEach((id, index) => {
            this.addWidget(id, { x: (index * 3), y: 0 });
        });

        this.addWidget('wallet-growth', { x: 0, y: 2 });
        this.addWidget('credential-types', { x: 6, y: 2 });
        this.addWidget('recent-wallets', { x: 0, y: 6 });
        this.addWidget('live-feed', { x: 6, y: 6 });
    }

    resetLayout() {
        // Clear current layout
        this.gridstack.removeAll();
        this.widgets.clear();

        // Load defaults
        this.loadDefaultLayout();
    }

    setupEventListeners() {
        // Grid change events
        this.gridstack.on('change', () => {
            this.saveLayout();
        });

        // Widget gallery toggle
        document.getElementById('widget-gallery-toggle')?.addEventListener('click', () => {
            this.toggleWidgetGallery();
        });
    }

    toggleWidgetGallery() {
        const gallery = document.getElementById('widget-gallery');
        if (gallery) {
            gallery.classList.toggle('open');
            if (gallery.classList.contains('open')) {
                this.renderWidgetGallery();
            }
        }
    }

    renderWidgetGallery() {
        const gallery = document.getElementById('widget-gallery-items');
        if (!gallery) return;

        gallery.innerHTML = '';

        // Group widgets by type
        const grouped = {};
        this.availableWidgets.forEach((widget) => {
            if (!grouped[widget.type]) {
                grouped[widget.type] = [];
            }
            grouped[widget.type].push(widget);
        });

        // Render groups
        Object.entries(grouped).forEach(([type, widgets]) => {
            const section = document.createElement('div');
            section.className = 'gallery-section';
            section.innerHTML = `<h4>${type.charAt(0).toUpperCase() + type.slice(1)} Widgets</h4>`;

            const items = document.createElement('div');
            items.className = 'gallery-items';

            widgets.forEach(widget => {
                const item = document.createElement('div');
                item.className = 'gallery-item';
                item.innerHTML = `
                    <span class="gallery-icon">${widget.icon}</span>
                    <span class="gallery-name">${widget.name}</span>
                    <button class="gallery-add" onclick="dashboardManager.addWidgetFromGallery('${widget.id}')">
                        <i class="fas fa-plus"></i>
                    </button>
                `;
                items.appendChild(item);
            });

            section.appendChild(items);
            gallery.appendChild(section);
        });
    }

    addWidgetFromGallery(widgetId) {
        // Check if widget already exists
        if (this.widgets.has(widgetId)) {
            alert('This widget is already on the dashboard');
            return;
        }

        // Add widget
        this.addWidget(widgetId);

        // Close gallery
        document.getElementById('widget-gallery')?.classList.remove('open');
    }

    openConfigModal(widgetId) {
        // This would open a Blazor modal for widget configuration
        console.log(`Configure widget: ${widgetId}`);
    }

    startRealtimeFeed(widgetId) {
        // Simulate real-time updates
        const feedElement = document.querySelector(`[data-widget-id="${widgetId}"] .feed-items`);
        if (!feedElement) return;

        setInterval(() => {
            const item = document.createElement('div');
            item.className = 'feed-item fade-in';
            item.innerHTML = `
                <span class="feed-time">${new Date().toLocaleTimeString()}</span>
                <span class="feed-message">New wallet created</span>
            `;
            feedElement.insertBefore(item, feedElement.firstChild);

            // Limit items
            while (feedElement.children.length > 20) {
                feedElement.removeChild(feedElement.lastChild);
            }
        }, 5000);
    }

    loadMonitorData(widgetId) {
        const element = document.querySelector(`[data-widget-id="${widgetId}"] .health-indicators`);
        if (!element) return;

        element.innerHTML = `
            <div class="health-item health-good">
                <span class="health-label">API</span>
                <span class="health-status">●</span>
            </div>
            <div class="health-item health-good">
                <span class="health-label">Database</span>
                <span class="health-status">●</span>
            </div>
            <div class="health-item health-warning">
                <span class="health-label">Cache</span>
                <span class="health-status">●</span>
            </div>
            <div class="health-item health-good">
                <span class="health-label">Queue</span>
                <span class="health-status">●</span>
            </div>
        `;
    }

    loadTableData(widgetId) {
        // Load mock table data
        const table = document.querySelector(`[data-widget-id="${widgetId}"] .widget-table tbody`);
        if (!table) return;

        const mockData = [
            ['John Doe', '2 mins ago', 'Active'],
            ['Jane Smith', '5 mins ago', 'Pending'],
            ['Bob Johnson', '10 mins ago', 'Active']
        ];

        table.innerHTML = mockData.map(row =>
            `<tr>${row.map(cell => `<td>${cell}</td>`).join('')}</tr>`
        ).join('');
    }
}

// Initialize on load
let dashboardManager;
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('dashboard-grid')) {
        dashboardManager = new DashboardWidgetManager('dashboard-grid');
    }
});