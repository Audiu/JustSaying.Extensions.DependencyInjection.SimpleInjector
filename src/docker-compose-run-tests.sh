#!/bin/bash

 docker-compose.exe -f docker-compose.yml up --build --abort-on-container-exit --exit-code-from testrunner
