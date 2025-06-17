FROM nginx:alpine

# Копируем новый HTML файл с красивым дизайном
COPY index.html /usr/share/nginx/html/index.html

# Копируем конфигурацию nginx
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"] 