# ── Stage 1: Build React app ────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --prefer-offline

COPY . .

# When built for K8s the API is proxied by nginx on the same origin (/api/...)
# so we use a relative path. Override with REACT_APP_API_URL=http://... for
# other environments.
ARG REACT_APP_API_URL=/api
ENV REACT_APP_API_URL=$REACT_APP_API_URL

RUN npm run build

# ── Stage 2: Serve with nginx ────────────────────────────────────────────────
FROM nginx:1.27-alpine AS final
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
