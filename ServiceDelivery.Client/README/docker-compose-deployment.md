
## Deploy app to local network with nginx access via https://localhost 
here's how you can host your app in Docker, served over https://localhost using Nginx as a reverse proxy with TLS. This setup is strictly for local development, using a self-signed cert or mkcert, and Nginx will serve HTTPS on port 443.