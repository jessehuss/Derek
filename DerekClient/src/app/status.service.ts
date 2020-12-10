import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Observable } from 'rxjs';

export enum SensorStatus {
  Unreachable,
  Secured,
  Unsecured
}

export class Sensor {
  NodeID: string = null;
  NodeLocation: string = null;
  NodeStatus: SensorStatus;
  constructor(init?: Partial<Sensor>) {
    Object.assign(this, init);
  }
}

@Injectable()
export class StatusService {

  private statusURL: string;
  constructor(private http: HttpClient, private zone: NgZone) {
    this.statusURL = `${window.location.origin.replace('4200', '9797').toLocaleLowerCase()}/api/status`;
  }
  
  getServerSentEvent(): Observable<any> {
    return new Observable(observer => {
      const eventSource = this.getEventSource();

      eventSource.onmessage = event => {
        this.zone.run(() => {
          observer.next(event);
        })
      }

      eventSource.onerror = error => {
        this.zone.run(() => {
          observer.error(error);
        })
      }
    })
  }

  private getEventSource(): EventSource {
    return new EventSource(this.statusURL);
  }
}
