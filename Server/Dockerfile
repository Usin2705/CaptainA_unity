# For more information, please refer to https://aka.ms/vscode-docker-python
FROM python:3.9.13-slim

# Keeps Python from generating .pyc files in the container
ENV PYTHONDONTWRITEBYTECODE=1

# Turns off buffering for easier container logging
ENV PYTHONUNBUFFERED=1

# Install pip requirements
COPY requirements.txt .

# Install any needed packages specified in requirements.txt
RUN pip install --trusted-host pypi.python.org -r requirements.txt
RUN apt-get update && apt-get install -y ffmpeg

COPY . .

# Expose port 5000
EXPOSE 52705
ENV PORT 52705

#WORKDIR /app

# During debugging, this entry point will be overridden. For more information, please refer to https://aka.ms/vscode-docker-python-debug
#CMD ["gunicorn", "--bind", "0.0.0.0:5000", "main.main:app"]
CMD exec gunicorn --bind :$PORT main:app --workers 3 --threads 1 --timeout 0
#ENTRYPOINT ["python", "main.py"]
