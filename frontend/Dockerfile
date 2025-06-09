FROM nginx:alpine

# Копируем HTML файл
COPY api-test.html /usr/share/nginx/html/index.html

# Копируем конфигурацию nginx
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"] 