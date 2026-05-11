// Production environment — points to Azure App Service backend
// This file is swapped in automatically when you run: ng build --configuration production
export const environment = {
  production: true,
  apiUrl: 'https://hr-agent-backend.azurewebsites.net/api/chat'
};
