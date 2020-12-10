import { Component, OnInit } from '@angular/core';
import { SensorStatus, Sensor, StatusService } from './status.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'DerekClient';
  _status = SensorStatus;
  data: Sensor[] = [];
  constructor(private svc: StatusService) {}

  ngOnInit() {
    this.svc.getServerSentEvent()
    .subscribe(
      response => this.parseData(response),
      error => console.log(error)
    );
  }

  private parseData(resp: any) {
    const data = resp.data;

    const parsedData = JSON.parse(data);
    console.log(parsedData);
    this.data = parsedData;
  }
}
