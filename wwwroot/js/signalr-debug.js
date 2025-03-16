/**
 * SignalR Debug Helper
 * Este archivo ayuda a diagnosticar problemas con SignalR
 */

// Objeto para guardar el estado de la depuración
const SignalRDebug = {
    // Estado de la conexión
    connectionState: {
        question: null,
        notification: null
    },
    
    // Registros de eventos
    logs: [],
    
    // Contador de intentos para comprobar SignalR
    signalRCheckAttempts: 0,
    devCommunityCheckAttempts: 0,
    
    // Estado de autenticación
    isAuthenticated: false,
    
    // Inicializar depuración
    init: function() {
        console.log("SignalR Debug: Inicializando...");
        this.createDebugPanel();
        
        // Check authentication status
        this.checkAuthenticationStatus();
        
        // Primero verificar si SignalR está disponible
        this.checkSignalRAvailability();
    },
    
    // Verificar si el usuario está autenticado
    checkAuthenticationStatus: function() {
        // Simple checks for authentication elements
        const logoutForm = document.querySelector('form#logoutForm');
        const userDropdown = document.querySelector('.nav-item.dropdown:has(.dropdown-item[onclick*="logoutForm"])');
        
        this.isAuthenticated = (logoutForm !== null || userDropdown !== null);
        
        if (this.isAuthenticated) {
            this.logSuccess("Usuario autenticado");
        } else {
            this.logWarning("Usuario no autenticado - NotificationHub requiere autenticación");
        }
    },
    
    // Verificar si la librería SignalR está disponible
    checkSignalRAvailability: function() {
        if (typeof signalR !== 'undefined') {
            console.log("SignalR Debug: Librería SignalR encontrada");
            this.logSuccess("Librería SignalR cargada correctamente");
            this.checkDevCommunitySignalR();
            return;
        }
        
        this.signalRCheckAttempts++;
        
        if (this.signalRCheckAttempts <= 5) {
            this.logWarning(`Librería SignalR no encontrada. Intento ${this.signalRCheckAttempts}/5`);
            
            // Intentar nuevamente después de un retraso
            setTimeout(() => {
                this.checkSignalRAvailability();
            }, 2000);
        } else {
            this.logError("No se pudo encontrar la librería SignalR después de varios intentos");
        }
    },
    
    // Verificar si DevCommunitySignalR está disponible
    checkDevCommunitySignalR: function() {
        if (window.DevCommunitySignalR) {
            console.log("SignalR Debug: Objeto DevCommunitySignalR encontrado");
            this.logSuccess("Objeto DevCommunitySignalR disponible");
            
            // Verificar si el objeto DevCommunitySignalR está inicializado
            if (window.DevCommunitySignalR.questionConnection || window.DevCommunitySignalR.notificationConnection) {
                // Ya está inicializado, monitor connections now
                this.monitorConnections();
            } else {
                // Esperamos un poco a que se inicialice
                this.logInfo("Esperando inicialización de DevCommunitySignalR...");
                setTimeout(() => {
                    this.monitorConnections();
                }, 2000);
            }
            
            return;
        }
        
        this.devCommunityCheckAttempts++;
        
        if (this.devCommunityCheckAttempts <= 10) {  // Aumentado a 10 intentos
            this.logWarning(`Objeto DevCommunitySignalR no encontrado. Intento ${this.devCommunityCheckAttempts}/10`);
            
            // Intentar nuevamente después de un retraso
            setTimeout(() => {
                this.checkDevCommunitySignalR();
            }, 3000);  // Aumentado el tiempo de espera entre intentos
        } else {
            this.logError("No se pudo encontrar DevCommunitySignalR después de varios intentos");
            this.logInfo("Intentando inicializar manualmente DevCommunitySignalR...");
            
            // Comprobación adicional - podría ser que el objeto exista pero no haya sido inicializado
            if (typeof DevCommunitySignalR !== 'undefined') {
                this.logInfo("Objeto DevCommunitySignalR existe pero no estaba en window - inicializando");
                window.DevCommunitySignalR = DevCommunitySignalR;
                
                // Intentar inicializar si tiene método init
                if (typeof DevCommunitySignalR.init === 'function') {
                    try {
                        DevCommunitySignalR.init();
                        this.logSuccess("DevCommunitySignalR inicializado manualmente");
                        setTimeout(() => {
                            this.monitorConnections();
                        }, 2000);
                    } catch (e) {
                        this.logError(`Error al inicializar manualmente: ${e.message}`);
                    }
                }
            } else {
                this.logInfo("Continuar con funcionalidad limitada");
                // Intentar monitorear connections de todos modos con un stub para evitar errores
                this.connectionState.question = "No disponible";
                this.connectionState.notification = "No disponible";
                this.updateDebugPanel();
            }
        }
    },
    
    // Monitorear las conexiones en intervalos
    monitorConnections: function() {
        // Comprobar inmediatamente
        this.checkHubConnections();
        
        // Establecer intervalos para verificar el estado actual
        setInterval(() => {
            this.checkHubConnections();
        }, 3000);
    },
    
    // Verificar las conexiones de los hubs
    checkHubConnections: function() {
        // Verificar la conexión del notification hub
        try {
            if (window.DevCommunitySignalR && window.DevCommunitySignalR.notificationConnection) {
                const state = window.DevCommunitySignalR.notificationConnection.state;
                this.connectionState.notification = state;
                this.updateDebugPanel();
                
                if (state !== signalR.HubConnectionState.Connected) {
                    this.logWarning(`NotificationHub no está conectado. Estado: ${state}`);
                }
            } else {
                this.connectionState.notification = "No inicializado";
                this.logError("NotificationHub no está inicializado. Revise signalr-client.js");
            }
        } catch (e) {
            this.logError(`Error al verificar NotificationHub: ${e.message}`);
        }
        
        // Verificar la conexión del question hub
        try {
            if (window.DevCommunitySignalR && window.DevCommunitySignalR.questionConnection) {
                const state = window.DevCommunitySignalR.questionConnection.state;
                this.connectionState.question = state;
                this.updateDebugPanel();
                
                if (state !== signalR.HubConnectionState.Connected) {
                    this.logWarning(`QuestionHub no está conectado. Estado: ${state}`);
                }
            } else {
                this.connectionState.question = "No inicializado";
                this.logError("QuestionHub no está inicializado. Revise signalr-client.js");
            }
        } catch (e) {
            this.logError(`Error al verificar QuestionHub: ${e.message}`);
        }
    },
    
    // Probar conexiones directamente
    testConnections: function() {
        try {
            console.log("SignalR Debug: Probando conexión con NotificationHub...");
            
            // Check authentication first for NotificationHub
            if (!this.isAuthenticated) {
                this.logWarning("No se puede probar NotificationHub - Usuario no autenticado");
                return;
            }
            
            // Crear una conexión de prueba al NotificationHub con autenticación
            const testConnection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub", {
                    // Pass authentication tokens if available
                    accessTokenFactory: () => {
                        return this.getAuthToken();
                    }
                })
                .build();
                
            testConnection.onclose(error => {
                this.logError(`Conexión de prueba cerrada: ${error ? error.message : "Sin error"}`);
            });
            
            testConnection.start()
                .then(() => {
                    this.logSuccess("Conexión de prueba a NotificationHub exitosa");
                    
                    // Cerrar la conexión después de probarla
                    setTimeout(() => {
                        testConnection.stop();
                    }, 5000);
                })
                .catch(error => {
                    this.logError(`Error en conexión de prueba: ${error.message}`);
                });
        } catch (e) {
            this.logError(`Error general al probar conexiones: ${e.message}`);
        }
    },
    
    // Obtener token de autenticación
    getAuthToken: function() {
        // Try to get from cookie
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].trim();
            if (cookie.startsWith('.AspNetCore.Identity.Application=') || 
                cookie.startsWith('Authorization=') ||
                cookie.startsWith('DevCommunityAuth=')) {
                return cookie.split('=')[1];
            }
        }
        
        // Or from localStorage if your app uses that
        const token = localStorage.getItem('auth_token');
        if (token) return token;
        
        // Return null if no token found
        return null;
    },
    
    // Crear panel de depuración
    createDebugPanel: function() {
        console.log("SignalR Debug: Creando panel de depuración...");
        
        // Crear elemento del panel
        const panel = document.createElement('div');
        panel.id = 'signalr-debug-panel';
        panel.style.cssText = `
            position: fixed;
            bottom: 10px;
            left: 10px;
            z-index: 9999;
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 10px;
            border-radius: 5px;
            font-family: monospace;
            max-width: 500px;
            max-height: 300px;
            overflow: auto;
        `;
        
        // Contenido inicial
        panel.innerHTML = `
            <div style="margin-bottom: 5px; font-weight: bold;">SignalR Debug</div>
            <div id="signalr-conn-status"></div>
            <div id="signalr-debug-logs" style="margin-top: 10px; font-size: 11px;"></div>
            <div style="margin-top: 10px;">
                <button id="signalr-test-btn" style="font-size: 11px; padding: 2px 5px;">Test Connection</button>
                <button id="signalr-close-btn" style="font-size: 11px; padding: 2px 5px; margin-left: 5px;">Close</button>
            </div>
        `;
        
        // Agregar panel al body
        document.body.appendChild(panel);
        
        // Configurar botones
        document.getElementById('signalr-test-btn').addEventListener('click', () => this.testConnections());
        document.getElementById('signalr-close-btn').addEventListener('click', () => document.getElementById('signalr-debug-panel').remove());
        
        // Registrar creación
        this.logInfo("Panel de depuración creado");
    },
    
    // Actualizar el panel de depuración
    updateDebugPanel: function() {
        const statusEl = document.getElementById('signalr-conn-status');
        if (!statusEl) return;
        
        statusEl.innerHTML = `
            <div>NotificationHub: <span style="color: ${this.getStateColor(this.connectionState.notification)}">${this.connectionState.notification}</span></div>
            <div>QuestionHub: <span style="color: ${this.getStateColor(this.connectionState.question)}">${this.connectionState.question}</span></div>
        `;
    },
    
    // Obtener color según el estado
    getStateColor: function(state) {
        if (state === signalR.HubConnectionState.Connected) return 'lime';
        if (state === signalR.HubConnectionState.Connecting) return 'yellow';
        if (state === signalR.HubConnectionState.Disconnected) return 'red';
        if (state === signalR.HubConnectionState.Reconnecting) return 'orange';
        return 'gray';
    },
    
    // Funciones de registro
    logInfo: function(message) {
        this.addLogEntry('INFO', message, 'white');
    },
    
    logSuccess: function(message) {
        this.addLogEntry('SUCCESS', message, 'lime');
    },
    
    logWarning: function(message) {
        this.addLogEntry('WARNING', message, 'yellow');
    },
    
    logError: function(message) {
        this.addLogEntry('ERROR', message, 'red');
        console.error(`SignalR Debug ERROR: ${message}`);
    },
    
    // Agregar entrada de registro
    addLogEntry: function(level, message, color) {
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = { level, message, timestamp };
        
        // Almacenar en el historial de registros
        this.logs.push(logEntry);
        
        // Limitar a 100 registros
        if (this.logs.length > 100) {
            this.logs.shift();
        }
        
        // Actualizar panel de depuración
        const logsEl = document.getElementById('signalr-debug-logs');
        if (logsEl) {
            const logHtml = `<div>[${timestamp}] <span style="color: ${color}">${level}</span>: ${message}</div>`;
            logsEl.insertAdjacentHTML('afterbegin', logHtml);
        }
    }
};

// Iniciar depuración cuando se cargue la página
document.addEventListener('DOMContentLoaded', function() {
    // Esperar más tiempo para que otras scripts se carguen primero
    setTimeout(function() {
        // Comprobar primero si el objeto DevCommunitySignalR ya existe
        if (window.DevCommunitySignalR) {
            console.log("SignalR Debug: DevCommunitySignalR ya existe, inicializando depurador...");
        } else {
            console.log("SignalR Debug: Esperando a que DevCommunitySignalR se cargue...");
        }
        
        // Inicializar el depurador
        SignalRDebug.init();
    }, 3000); // Aumentado a 3 segundos para permitir suficiente tiempo de carga
}); 