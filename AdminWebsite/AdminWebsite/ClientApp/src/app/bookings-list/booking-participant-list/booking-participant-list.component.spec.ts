import { DebugElement, Component, Input } from '@angular/core';
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RouterTestingModule } from '@angular/router/testing';

import { BookingParticipantListComponent } from './booking-participant-list.component';
import { ParticipantDetailsModel } from '../../common/model/participant-details.model';

@Component({
  selector: 'app-booking-participant-details',
  template: ''
})
class ParticipantDetailsMockComponent {
  @Input()
  participant: ParticipantDetailsModel = null;

  @Input()
  vh_officer_admin: boolean;
}

describe('BookingParticipantListComponent', () => {

  let component: BookingParticipantListComponent;
  let fixture: ComponentFixture<BookingParticipantListComponent>;
  let debugElement: DebugElement;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [BookingParticipantListComponent, ParticipantDetailsMockComponent],
      imports: [RouterTestingModule],
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BookingParticipantListComponent);
    debugElement = fixture.debugElement;
    component = debugElement.componentInstance;

    fixture.detectChanges();
  });

  it('should create component', (() => {
    expect(component).toBeTruthy();
  }));

  it('should display participants list', (done => {
    const pr1 = new ParticipantDetailsModel('1', 'Mrs', 'Alan', 'Brake', 'Citizen', 'email.p1@email.com',
      'email1@co.uk', 'Defendant', 'Defendant LIP', 'Alan Brake', '');
    const participantsList: Array<ParticipantDetailsModel> = [];
    participantsList.push(pr1);
    participantsList.push(pr1);
    participantsList.push(pr1);

    component.participants = participantsList;

    fixture.whenStable().then(
      () => {
        fixture.detectChanges();
        const divElementRole = debugElement.queryAll(By.css('#participants-list > div'));
        expect(divElementRole.length).toBeGreaterThan(0);
        expect(divElementRole.length).toBe(3);
        done();
      }
    );
  }));
  it('should detect last item in the participants list', (done => {
    const pr1 = new ParticipantDetailsModel('1', 'Mrs', 'Alan', 'Brake', 'Citizen', 'email.p1@email.com',
      'email1@co.uk', 'Defendant', 'Defendant LIP', 'Alan Brake', '');
    const participantsList: Array<ParticipantDetailsModel> = [];
    participantsList.push(pr1);
    participantsList.push(pr1);
    participantsList.push(pr1);

    component.participants = participantsList;

    fixture.whenStable().then(
      () => {
        fixture.detectChanges();
        expect(component.participants[2].Flag).toBeTruthy();
        done();
      }
    );
  }));

  it('should display judges list', (done => {
    const pr1 = new ParticipantDetailsModel('1', 'Mrs', 'Alan', 'Brake', 'Judge', 'email.p1@email.com',
      'email1@co.uk', 'Judge', 'Judge', 'Alan Brake', '');
    const participantsList: Array<ParticipantDetailsModel> = [];
    participantsList.push(pr1);
    participantsList.push(pr1);

    component.judges = participantsList;

    fixture.whenStable().then(
      () => {
        fixture.detectChanges();
        const divElementRole = debugElement.queryAll(By.css('#judges-list > div'));
        expect(divElementRole.length).toBeGreaterThan(0);
        expect(divElementRole.length).toBe(2);
        done();
      }
    );
  }));
});
