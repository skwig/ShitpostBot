#alpine doesnt support (?) tensorflow
FROM python:3.10.4-slim
WORKDIR /app

ADD requirements.txt /app
RUN pip install -r requirements.txt

ADD . /app
EXPOSE 5000
CMD ["python","app.py"]