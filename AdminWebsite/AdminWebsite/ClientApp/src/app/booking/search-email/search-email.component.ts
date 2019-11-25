import { Component, EventEmitter, Output, OnInit, Input } from '@angular/core';
import { Subject } from 'rxjs';
import { PersonResponse } from '../../services/clients/api-client';
import { Constants } from '../../common/constants';
import { ParticipantModel } from '../../common/model/participant.model';
import { SearchService } from '../../services/search.service';

@Component({
    selector: 'app-search-email',
    templateUrl: './search-email.component.html',
    styleUrls: ['./search-email.component.css'],
    providers: [SearchService]
})
export class SearchEmailComponent implements OnInit {
    constants = Constants;
    participantDetails: ParticipantModel;
    searchTerm = new Subject<string>();
    results: ParticipantModel[] = [];
    isShowResult = false;
    notFoundParticipant = false;
    email = '';
    isValidEmail = true;

    @Input()
    disabled = true;

    @Output()
    findParticipant = new EventEmitter<ParticipantModel>();

    @Output()
    participantsNotFound = new EventEmitter();

    @Output()
    emailChanged = new EventEmitter<string>();

    constructor(private searchService: SearchService) { }

    ngOnInit() {
        this.searchService.search(this.searchTerm)
            .subscribe(data => {
                if (data && data.length > 0) {
                    this.getData(data);
                } else {
                    if (this.email.length > 2) {
                        this.noDataFound();
                    } else {
                        this.lessThanThreeLetters();
                    }
                    this.isShowResult = false;
                    this.results = undefined;
                }
            });

        this.searchTerm.subscribe(s => this.email = s);
    }

    getData(data: PersonResponse[]) {
        this.results = data.map(x => this.mapPersonResponseToParticipantModel(x));
        this.isShowResult = true;
        this.isValidEmail = true;
        this.notFoundParticipant = false;
    }

    noDataFound() {
        this.isShowResult = false;
        this.notFoundParticipant = true;
        this.participantsNotFound.emit();
    }

    lessThanThreeLetters() {
        this.isShowResult = false;
        this.notFoundParticipant = false;
    }

    selectItemClick(result: ParticipantModel) {
        this.email = result.email;

        const selectedResult = new ParticipantModel();
        selectedResult.email = result.email;
        selectedResult.first_name = result.first_name;
        selectedResult.last_name = result.last_name;
        selectedResult.title = result.title;
        selectedResult.phone = result.phone;
        selectedResult.company = result.company;
        selectedResult.housenumber = result.housenumber;
        selectedResult.street = result.street;
        selectedResult.city = result.city;
        selectedResult.county = result.county;
        selectedResult.postcode = result.postcode;
        selectedResult.is_exist_person = true;
        this.isShowResult = false;
        this.findParticipant.emit(selectedResult);
    }

    validateEmail() {
        /* tslint:disable: max-line-length */
        const pattern = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;
        this.isValidEmail = this.email && this.email.length > 0 && this.email.length < 256 && pattern.test(this.email.toLowerCase());
        return this.isValidEmail;
    }

    blur() {
        this.isShowResult = false;
    }

    clearEmail() {
        this.email = '';
        this.isValidEmail = true;
        this.notFoundParticipant = false;
    }

    blurEmail() {
        if (!this.results || this.results.length === 0) {
            this.validateEmail();
            this.emailChanged.emit(this.email);
            this.notFoundParticipant = false;
        }
    }

    mapPersonResponseToParticipantModel(p: PersonResponse): ParticipantModel {
        let participant: ParticipantModel;
        if (p) {
            participant = new ParticipantModel();
            participant.id = p.id;
            participant.title = p.title;
            participant.first_name = p.first_name;
            participant.middle_names = p.middle_names;
            participant.last_name = p.last_name;
            participant.username = p.username;
            participant.email = p.contact_email;
            participant.phone = p.telephone_number;
            participant.representee = '';
            participant.solicitorsReference = '';
            participant.company = p.organisation;
            participant.housenumber = p.house_number;
            participant.street = p.street;
            participant.city = p.city;
            participant.county = p.county;
            participant.postcode = p.postcode;
        }

        return participant;
    }
}
