# For more information, please refer to https://aka.ms/vscode-docker-python
FROM huggingface/transformers-pytorch-cpu:4.18.0

# Keeps Python from generating .pyc files in the container
ENV PYTHONDONTWRITEBYTECODE=1

# Turns off buffering for easier container logging
ENV PYTHONUNBUFFERED=1

# Install pip requirements
COPY requirements.txt .

# Install any needed packages specified in requirements.txt
RUN pip install --trusted-host pypi.python.org -r requirements.txt

# Change docker language to deal with UTF-8
ENV LANG C.UTF-8
ENV LC_ALL C.UTF-8

COPY . /app/

# Expose port 52705
EXPOSE 52705
ENV PORT 52705

WORKDIR /app

# During debugging, this entry point will be overridden. For more information, please refer to https://aka.ms/vscode-docker-python-debug
#CMD ["gunicorn", "--bind", "0.0.0.0:5000", "main.main:app"]
RUN mkdir -p -v logs
CMD exec gunicorn --bind :$PORT main:app --workers 2 --threads 1 --access-logfile /app/logs/gunicorn-access.log --error-logfile /app/logs/gunicorn-error.log --capture-output --log-level debug
# 2 workers with 300mil model, 6 workers with 95mil models
# -timeout should be high the bigger the model, default is 30s
#ENTRYPOINT ["python", "main.py"]