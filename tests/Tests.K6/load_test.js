import http from 'k6/http';
import { check, sleep } from 'k6';

// 1. Import the HTML reporter and standard text summary
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

export let options = {
    stages: [
        { duration: '30s', target: 20 }, // target 20 users
        { duration: '1m', target: 20 },
        { duration: '30s', target: 0 }  // ramp down to 0 users
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    },
};

export default function () {
    const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5173';

    // 1. Get watering schedule for a garden
    let res = http.get(`${BASE_URL}/api/gardens/1/watering-schedule`);
    check(res, {
        'is status 200': (r) => r.status === 200,
        'has correctly-formatted response': (r) => {
            if (r.status !== 200) return false;
            let body = r.json();
            return body !== null && body.length >= 0;
        },
    });

    // 2. Get ready to harvest plantings
    res = http.get(`${BASE_URL}/api/plantings/ready-to-harvest`);
    check(res, {
        'is status 200': (r) => r.status === 200,
    });

    sleep(1);
}

// 2. Add the handleSummary hook to generate the files at the end of the test
export function handleSummary(data) {
    return {
        // This will save the HTML report to your current folder
        "summary_load.html": htmlReport(data),

        // This ensures you still see the normal text output in your terminal
        stdout: textSummary(data, { indent: " ", enableColors: true }),
    };
}