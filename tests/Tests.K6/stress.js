import http from 'k6/http';
import { check, sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

export let options = {
    stages: [
        { duration: '30s', target: 50 }, // ramp up to 50 users
        { duration: '1m', target: 50 },
        { duration: '30s', target: 100 }, // ramp up to 100 users
        { duration: '1m', target: 100 },
        { duration: '30s', target: 0 },   // ramp down to 0
    ],
    thresholds: {
        http_req_duration: ['p(95)<2000'], // 95% of requests must complete below 2s under stress
        http_req_failed: ['rate<0.05'],    // less than 5% errors
    },
};

export default function () {
    const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5173';

    // 1. Get watering schedule for a garden
    let res = http.get(`${BASE_URL}/api/gardens/1/watering-schedule`);
    check(res, {
        'is status 200': (r) => r.status === 200,
    });

    // 2. Get ready to harvest plantings
    res = http.get(`${BASE_URL}/api/plantings/ready-to-harvest`);
    check(res, {
        'is status 200': (r) => r.status === 200,
    });

    sleep(1);
}

export function handleSummary(data) {
    return {
        "summary_stress.html": htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
    };
}
